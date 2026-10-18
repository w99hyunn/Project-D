using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
namespace DoHwa.UI.Editor
{
    public static class UiAnimatorAudit
    {
        private static readonly List<string> lines=new();
        private static readonly List<string> errors=new();
        private static string report="Tools/UIReview/AnimatorMigrationAudit.txt";
        private static void Log(string value){lines.Add(value);File.WriteAllLines(report,lines);}
        private static void Check(bool ok,string message){if(!ok)throw new Exception(message);Log("PASS "+message);}
        private static void CaptureLog(string text,string stack,LogType type)
        {if(type==LogType.Error || type==LogType.Exception || text.Contains("Animator is not playing") || text.Contains("has no receiver"))errors.Add(text);}
        private static async Awaitable Wait(float seconds)
        {float until=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<until)await Awaitable.NextFrameAsync();}
        private static T Field<T>(UnityEngine.Object owner,string name) where T:UnityEngine.Object=>(T)new SerializedObject(owner).FindProperty(name).objectReferenceValue;
        private static async Awaitable Scene(string name,float timeout=20)
        {
            float end=Time.realtimeSinceStartup+timeout;
            while(SceneManager.GetActiveScene().name!=name){if(Time.realtimeSinceStartup>end)throw new Exception("Scene timeout "+name);await Awaitable.NextFrameAsync();}
            await Wait(.7f);
        }
        public static async void Run()
        {
            lines.Clear();errors.Clear();Log("RUNNING Animator migration runtime audit");
            Application.logMessageReceived+=CaptureLog;
            try
            {
                if(SceneManager.GetActiveScene().name=="Splash")
                {
                    await Scene("Login");Check(true,"Splash sequence Animation Event reaches Login");
                    var login=UnityEngine.Object.FindFirstObjectByType<LoginUtilityPanel>();
                    login.OpenSettings();await Wait(.75f);
                    Check(Field<GameObject>(login,"settings").activeSelf,"Login settings opens");
                    login.CloseSettings();await Wait(.4f);
                    Check(!Field<GameObject>(login,"settings").activeSelf,"Login settings closes through clip event");
                    login.OpenNotices();await Wait(.7f);login.OpenLanguage();await Wait(.7f);login.CloseLanguage();await Wait(.4f);
                    login.OpenExit();await Wait(.7f);login.CloseExit();await Wait(.4f);
                    Check(!Field<GameObject>(login,"exit").activeSelf,"Login notices/language/exit repeated transitions");
                    UnityEngine.Object.FindFirstObjectByType<LoginEntry>().Enter();await Scene("Ingame");
                    Check(!UnityEngine.Object.FindFirstObjectByType<UiSceneTransition>(),"Authored scene curtain completes and disposes");
                }
                var root=GameObject.Find("RpgCanvas");root.TryGetComponent<RpgUiController>(out var ui);
                ui.OpenInventory();await Wait(.8f);
                var inventory=root.transform.Find("Inventory");inventory.TryGetComponent<CanvasGroup>(out var cg);
                Check(cg.interactable && cg.alpha>.99f,"Inventory opens and becomes interactive");
                var cards=inventory.Find("Grid").GetComponentsInChildren<RpgItemButton>(true);
                Check(cards.Length==36,"36 storage slots preserved");
                var card=cards[0];card.TryGetComponent<Animator>(out var animator);
                int layer=animator.GetLayerIndex("Item appearance");
                Check(animator.GetBool("ItemSelected") && animator.GetCurrentAnimatorStateInfo(layer).IsName("ChosenRest"),"Selected card survives inactive-to-active transition");
                Check(Vector4.Distance(Field<Image>(card,"cardFace").color,new Color(.59f,.76f,.90f))<.01f,"Selected card clip writes the intended face color");
                ScreenCapture.CaptureScreenshot("Tools/UIReview/AnimatorCardsVerified.png");await Wait(.08f);
                var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                ExecuteEvents.Execute<IPointerEnterHandler>(card.gameObject,pointer,ExecuteEvents.pointerEnterHandler);await Wait(.22f);
                Check(animator.GetCurrentAnimatorStateInfo(layer).IsName("ChosenHover"),"Selected + hover composition");
                ExecuteEvents.Execute<IPointerDownHandler>(card.gameObject,pointer,ExecuteEvents.pointerDownHandler);await Wait(.22f);
                Check(animator.GetCurrentAnimatorStateInfo(layer).IsName("ChosenPress"),"Pressed card state");
                ExecuteEvents.Execute<IPointerUpHandler>(card.gameObject,pointer,ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute<IPointerExitHandler>(card.gameObject,pointer,ExecuteEvents.pointerExitHandler);await Wait(.22f);
                Check(animator.GetCurrentAnimatorStateInfo(layer).IsName("ChosenRest"),"Release and pointer exit restore selected appearance");
                card.PulseItem(0);await Wait(.3f);
                Check(card.GetComponentsInChildren<Image>(true).Any(x=>x.name.EndsWith("ResultHighlight") && x.color.a>.3f),"Result emphasis clip reaches visible peak");
                ui.Select(1);await Wait(.5f);
                Check(!animator.GetBool("ItemSelected") && animator.GetCurrentAnimatorStateInfo(layer).IsName("DefaultRest"),"Selection change during emphasis preserves hover/selection state");
                Check(Vector4.Distance(Field<Image>(card,"cardFace").color,new Color(.82f,.86f,.92f))<.01f,"Shared controller keeps each card selection independent");
                Check(card.GetComponentsInChildren<Image>(true).Where(x=>x.name.EndsWith("ResultHighlight")).All(x=>x.color.a<.01f),"Emphasis returns fully transparent");
                ui.Sort();await Wait(.25f);inventory.TryGetComponent<InventorySortMenu>(out var sort);
                Check(sort.IsOpen,"Sort dropdown opens");sort.Hide();ui.SetSort(2);await Wait(.3f);ui.SetSort(0);
                ui.SetFilter(1);await Wait(.3f);ui.SetFilter(0);await Wait(.3f);
                var scroll=inventory.Find("Grid").GetComponentInChildren<SmoothInventoryScrollRect>(true);
                float before=scroll.verticalNormalizedPosition;
                scroll.OnScroll(new PointerEventData(EventSystem.current){scrollDelta=new Vector2(0,-3)});
                await Wait(.04f);float early=scroll.verticalNormalizedPosition;await Wait(.4f);float late=scroll.verticalNormalizedPosition;
                Check(early<before && late<early,"Smoothed wheel scroll still progresses");
                scroll.verticalScrollbar.TryGetComponent<Image>(out var track);
                Check(track.color.a==0,"Scrollbar track remains transparent");
                ui.Select(1);ui.UseSelected();await Wait(.7f);
                Check(ui.EquippedAt(0)==1,"Equipment data changes through existing action");
                ui.OpenEquipment();await Wait(.85f);
                var slots=root.GetComponentsInChildren<EquipmentSlotFeedback>(true);
                var occupied=slots.First(x=>{x.TryGetComponent<Animator>(out var a);return a.GetBool("Occupied");});
                occupied.TryGetComponent<Animator>(out var sa);
                occupied.TryGetComponent<Image>(out var slotImage);
                var art=(RectTransform)occupied.transform.Find("Art");float y=art.anchoredPosition.y;float effectTime=slotImage.material.GetFloat("_EffectTime");
                await Wait(.3f);
                Check(Mathf.Abs(art.anchoredPosition.y-y)>.01f && slotImage.material.GetFloat("_EffectTime")>effectTime,"Equipment float clip and unscaled shader clock move");
                occupied.OnPointerEnter(pointer);await Wait(.22f);
                Check(slotImage.material.GetFloat("_Focus")>.9f,"Equipment shader hover driven by Animator");
                occupied.OnPointerDown(pointer);await Wait(.22f);Check(slotImage.material.GetFloat("_Press")>.9f,"Equipment press clip reaches shader");
                occupied.OnPointerUp(pointer);occupied.OnPointerExit(pointer);occupied.Pulse();await Wait(.3f);
                Check(slotImage.material.GetFloat("_Focus")>.8f,"Equipment result emphasis driven by clip");
                ui.UseSelected();await Wait(.3f);Check(ui.EquippedAt(0)==-1,"Unequip operation preserved");
                ui.OpenInventory();await Wait(.8f);ui.Select(2);
                int quantity=ui.Catalog[2].quantity;ui.UseSelected();await Wait(.4f);
                Check(ui.Catalog[2].quantity==quantity-1,"Consumable quantity changes once");
                ui.Close();await Wait(.7f);
                var gauge=root.GetComponentsInChildren<UiGaugeMotion>(true).First(x=>x.name=="Fill");
                gauge.SetValue(.2f);await Wait(.5f);
                Check(Mathf.Abs(Field<Image>(gauge,"fill").fillAmount-.2f)<.01f,"Gauge value clip interpolates to runtime target");
                gauge.Pulse();await Wait(.32f);Check(new SerializedObject(gauge).FindProperty("resultPulse").floatValue>.5f,"Gauge emphasis clip");
                gauge.SetValue(.8f);await Wait(.5f);
                Check(Mathf.Abs(Field<Image>(gauge,"fill").fillAmount-.8f)<.01f,"Gauge recovers from low state");
                ui.OpenCrafting();await Wait(.8f);int herbs=ui.Catalog[6].quantity;int result=ui.Catalog[3].quantity;
                ui.UseSelected();await Wait(.08f);
                var dialog=root.GetComponentInChildren<CraftResultDialog>(true);
                Check(dialog.IsOpen && ui.Catalog[3].quantity==result+1 && ui.Catalog[6].quantity==herbs-3,"Crafting mutates quantities once and opens result");
                var item=dialog.transform.Find("Result/Item");item.TryGetComponent<CanvasGroup>(out var itemGroup);
                Check(itemGroup.alpha<.05f,"Craft result item waits for its animation cue");
                await Wait(.65f);dialog.TryGetComponent<CanvasGroup>(out var resultGroup);
                Check(resultGroup.interactable && itemGroup.alpha>.99f,"Craft result sequential reveal and input event");
                var confirm=dialog.GetComponentInChildren<Button>(true);
                ExecuteEvents.Execute<IPointerEnterHandler>(confirm.gameObject,pointer,ExecuteEvents.pointerEnterHandler);await Wait(.2f);
                confirm.TryGetComponent<Animator>(out var ca);Check(ca.GetCurrentAnimatorStateInfo(0).IsName("Highlighted"),"Confirm hover remains functional");
                confirm.onClick.Invoke();await Wait(.4f);Check(!dialog.IsOpen,"Confirm closes result through animation event");
                ui.OpenSettings();await Wait(.8f);
                var settings=root.GetComponentInChildren<SettingsMenu>(true);
                settings.ShowExit();await Wait(.7f);Check(settings.ExitOpen,"Exit confirmation opens");
                settings.CloseExit();await Wait(.4f);Check(!settings.ExitOpen,"Exit confirmation closes");
                ui.Close();await Wait(.6f);
                Check(!UiBackdropBlur.IsVisible,"Blur close event disables backdrop");
                for(int i=0;i<3;i++){ui.OpenInventory();await Wait(.7f);ui.Close();await Wait(.4f);}
                Check(!inventory.gameObject.activeSelf,"Repeated open/close has no stuck panel");
                Check(errors.Count==0,"No runtime exceptions or Animator binding warnings");
                ui.OpenEquipment();await Wait(.8f);
                ScreenCapture.CaptureScreenshot("Tools/UIReview/AnimatorMigrationVerified.png");await Wait(.3f);
                Log("PASS all checks");
            }
            catch(Exception exception){Log("FAIL "+exception);foreach(var error in errors.Distinct())Log(error);}
            finally{Application.logMessageReceived-=CaptureLog;EditorApplication.isPlaying=false;}
        }
    }
}
