using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DoHwa.UI
{
    public sealed class RpgUiController : MonoBehaviour
    {
        [Serializable]
        public class Item
        {
            public string title, category, description;
            public Sprite icon;
            public int quantity;
            public EquipmentPart equipmentPart=EquipmentPart.None;
        }

        [SerializeField] private Item[] items;
        [SerializeField] private HeroMotor motor;
        [SerializeField] private HeroCamera view;
        [SerializeField] private Transform hero;
        [SerializeField] private Camera minimapCamera;
        private GameObject inventory, map, journal, settings, character;
        private RectTransform minimapMarker;
        private TMP_Text title, category, description, count, actionLabel, healthLabel, foodLabel, feedback;
        private UnityEngine.UI.Image detailArt;
        private UiGaugeMotion healthMotion, foodMotion;
        private UnityEngine.UI.Button action;
        private RpgItemButton[] slots, quickSlots;
        private int selected, filter, equipped, quickSelected;
        private bool crafting, wasMotorEnabled, wasViewEnabled, modal;
        private float health = 80, food = 75, feedbackUntil;
        private readonly int[] quickItems = { 0, 1, 2, 3, 4, 6, 7, 9 };
        private InputAction bagKey, craftKey, mapKey, journalKey, escapeKey, equipmentKey;
        private InputAction[] quickKeys;
        private int transition;
        private int sortOrder;
        private InventorySortMenu sortMenu;
        private CraftResultDialog craftResult;
        private UiBackdropBlur backdropBlur;
        private bool capturingBackdrop;
        private bool equipmentMode;
        private int selectedPart;
        private EquipmentPanel equipmentPanel;
        private readonly int[] loadout={0,-1,-1,-1,-1,-1,-1,-1,-1};
        public Item[] Catalog => items;
        public bool IsModalOpen => modal;
        public int SelectedQuickItem => quickItems[quickSelected];
        private DoHwa.World.WorldRuntime world;

        public void BindWorld(DoHwa.World.WorldRuntime runtime) { world = runtime; }

        public void ApplyWorldState(DoHwa.World.SurvivorState player)
        {
            for (int i = 0; i < items.Length; i++) items[i].quantity = player.items[i];
            Array.Copy(player.equipment, loadout, loadout.Length);
            equipped = loadout[0]; health = player.health; food = player.food;
            Refresh();
        }

        public void ShowWorldResult(DoHwa.World.WorldCommand command, bool success, string reason)
        {
            if (!success) { ShowCondition(reason); return; }
            MarkItemChanged();
            if (command.action == DoHwa.World.WorldAction.Craft)
            {
                var recipe = world.Catalog.Recipe(command.target);
                craftResult.Show(items[recipe.result.item].icon, items[recipe.result.item].title, recipe.result.amount);
            }
        }
        public int EquippedAt(int part) => loadout[part];
        private bool IsEquipped(int id) => Array.IndexOf(loadout,id)>=0;
        private void Equip(int id) { loadout[(int)items[id].equipmentPart]=id; if(items[id].equipmentPart==EquipmentPart.Weapon)equipped=id; }

        public void Configure(Item[] catalog, HeroMotor movement, HeroCamera cameraView, Camera mini)
        {
            items = catalog; motor = movement; view = cameraView; hero = movement.transform; minimapCamera = mini;
        }

        private T Component<T>(string path) where T : Component
        {
            if(equipmentMode && path.StartsWith("Inventory/Detail"))path=path.Replace("Inventory/Detail","Character/Detail");
            transform.Find(path).TryGetComponent<T>(out var component);
            return component;
        }

        public void Bind()
        {
            inventory = transform.Find("Inventory").gameObject;
            map = transform.Find("Map").gameObject;
            journal = transform.Find("Journal").gameObject;
            settings = transform.Find("Settings").gameObject;
            character=transform.Find("Character").gameObject;
            equipmentPanel=Component<EquipmentPanel>("Character/EquipmentBoard");
            minimapMarker = (RectTransform)transform.Find("HUD/Region/MapFrame/Marker");
            title=Component<TMP_Text>("Inventory/Detail/Title");
            category=Component<TMP_Text>("Inventory/Detail/Category");
            description=Component<TMP_Text>("Inventory/Detail/Description");
            count=Component<TMP_Text>("Inventory/Detail/Count");
            actionLabel=Component<TMP_Text>("Inventory/Detail/Action/Label");
            action=Component<UnityEngine.UI.Button>("Inventory/Detail/Action");
            detailArt=Component<UnityEngine.UI.Image>("Inventory/Detail/Art");
            healthLabel=Component<TMP_Text>("HUD/Vitals/HealthText");
            foodLabel=Component<TMP_Text>("HUD/Vitals/FoodText");
            healthMotion=Component<UiGaugeMotion>("HUD/Vitals/HealthTrack/Fill");
            foodMotion=Component<UiGaugeMotion>("HUD/Vitals/FoodTrack/Fill");
            feedback=Component<TMP_Text>("Feedback/Label");
            slots=transform.Find("Inventory/Grid").GetComponentsInChildren<RpgItemButton>(true);
            quickSlots=transform.Find("HUD/Quickbar").GetComponentsInChildren<RpgItemButton>(true);
            Refresh();
        }

        private void Awake()
        {
            TryGetComponent<UiBackdropBlur>(out backdropBlur);
            Bind();
            inventory.TryGetComponent<InventorySortMenu>(out sortMenu);
            transform.Find("CraftResult").TryGetComponent<CraftResultDialog>(out craftResult);
            bagKey=new InputAction("Bag",InputActionType.Button,"<Keyboard>/b");
            craftKey=new InputAction("Craft",InputActionType.Button,"<Keyboard>/c");
            mapKey=new InputAction("Map",InputActionType.Button,"<Keyboard>/m");
            journalKey=new InputAction("Journal",InputActionType.Button,"<Keyboard>/j");
            escapeKey=new InputAction("Close",InputActionType.Button,"<Keyboard>/escape");
            equipmentKey=new InputAction("Equipment",InputActionType.Button,"<Keyboard>/k");
            quickKeys=Enumerable.Range(1,8).Select(i=>new InputAction("Quick"+i,InputActionType.Button,"<Keyboard>/"+i)).ToArray();
        }

        private void OnEnable()
        {
            bagKey.Enable(); craftKey.Enable(); mapKey.Enable(); journalKey.Enable(); escapeKey.Enable(); equipmentKey.Enable();
            foreach(var key in quickKeys) key.Enable();
        }

        private void OnDisable()
        {
            bagKey.Disable(); craftKey.Disable(); mapKey.Disable(); journalKey.Disable(); escapeKey.Disable(); equipmentKey.Disable();
            foreach(var key in quickKeys) key.Disable();
            if(modal) RestoreMovement();
        }

        private void OnDestroy()
        {
            bagKey.Dispose(); craftKey.Dispose(); mapKey.Dispose(); journalKey.Dispose(); escapeKey.Dispose(); equipmentKey.Dispose();
            foreach(var key in quickKeys) key.Dispose();
        }

        private void Update()
        {
            if(craftResult.IsOpen)
            {
                if(escapeKey.WasPressedThisFrame()) craftResult.Close();
                return;
            }
            if(bagKey.WasPressedThisFrame()) { if(inventory.activeSelf && !crafting) Close(); else OpenInventory(); }
            if(equipmentKey.WasPressedThisFrame()) { if(character.activeSelf) Close(); else OpenEquipment(); }
            if(craftKey.WasPressedThisFrame()) { if(inventory.activeSelf && crafting) Close(); else OpenCrafting(); }
            if(mapKey.WasPressedThisFrame()) { if(map.activeSelf) Close(); else OpenMap(); }
            if(journalKey.WasPressedThisFrame()) { if(journal.activeSelf) Close(); else OpenJournal(); }
            if(escapeKey.WasPressedThisFrame()) { if(modal) Close(); else OpenSettings(); }
            if(!modal) for(int i=0;i<quickKeys.Length;i++) if(quickKeys[i].WasPressedThisFrame()) UseQuick(i);
            if(feedback.transform.parent.gameObject.activeSelf && Time.unscaledTime>feedbackUntil) feedback.transform.parent.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            minimapCamera.transform.position=hero.position+Vector3.up*(map.activeSelf?90:45);
            minimapCamera.orthographicSize=map.activeSelf?35:14;
            float headingX=Vector3.Dot(motor.Facing,minimapCamera.transform.right);
            float headingY=Vector3.Dot(motor.Facing,minimapCamera.transform.up);
            minimapMarker.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(headingX,headingY)*Mathf.Rad2Deg);
        }

        private async void Show(GameObject panel)
        {
            int current=++transition;
            if(craftResult.IsOpen) craftResult.Close();
            bool alreadyOpen = panel.activeSelf;
            HideQuickTooltip();
            if(!modal) {
                wasMotorEnabled=motor.enabled; wasViewEnabled=view.enabled; motor.enabled=false; view.enabled=false; modal=true;
                transform.Find("HUD").gameObject.SetActive(false);
                capturingBackdrop=true;
                await backdropBlur.CaptureAsync();
                capturingBackdrop=false;
                if(!this) return;
                if(current!=transition) { if(!modal) backdropBlur.Hide(); return; }
            }
            while(capturingBackdrop) await Awaitable.NextFrameAsync(destroyCancellationToken);
            if(!this || current!=transition) return;
            GameObject outgoing = character.activeSelf ? character : inventory.activeSelf ? inventory : map.activeSelf ? map : journal.activeSelf ? journal : settings.activeSelf ? settings : null;
            if(outgoing && outgoing != panel && outgoing.TryGetComponent<UiPanelReveal>(out var exitMotion))
            {
                await exitMotion.HideAsync();
                if(!this || current != transition) return;
            }
            inventory.SetActive(panel==inventory); map.SetActive(panel==map); journal.SetActive(panel==journal); settings.SetActive(panel==settings);
            character.SetActive(panel==character);
            transform.Find("HUD").gameObject.SetActive(false);
            if(alreadyOpen && panel.TryGetComponent<UiPanelReveal>(out var reveal)) reveal.Replay();
        }

        private void RestoreMovement()
        {
            // Scene teardown may destroy the character before the UI is disabled.
            if(motor) motor.enabled=wasMotorEnabled;
            if(view) view.enabled=wasViewEnabled;
            modal=false;
        }
        public async void Close()
        {
            if(craftResult.IsOpen) { craftResult.Close(); return; }
            if(inventory.activeSelf && sortMenu.IsOpen) { sortMenu.Hide(); return; }
            if(settings.activeSelf && settings.TryGetComponent<SettingsMenu>(out var settingsMenu) && settingsMenu.ExitOpen)
            {
                settingsMenu.CloseExit();
                return;
            }
            int current = ++transition;
            GameObject panel = character.activeSelf ? character : inventory.activeSelf ? inventory : map.activeSelf ? map : journal.activeSelf ? journal : settings;
            if(panel.activeSelf && panel.TryGetComponent<UiPanelReveal>(out var reveal)) await reveal.HideAsync();
            if(!this || current != transition) return;
            inventory.SetActive(false); map.SetActive(false); journal.SetActive(false); settings.SetActive(false);
            character.SetActive(false);
            transform.Find("HUD").gameObject.SetActive(true);
            backdropBlur.Hide();
            if(modal) RestoreMovement();
        }

        public void OpenInventory() { equipmentMode=false; crafting=false; filter=0; Show(inventory); Select(0); ResetInventoryScroll(); }
        public void OpenCrafting() { equipmentMode=false; crafting=true; filter=0; Show(inventory); Select(3); ResetInventoryScroll(); }
        public void OpenEquipment() { equipmentMode=true;crafting=false;filter=0;selectedPart=0;selected=loadout[0]>=0?loadout[0]:0;Show(character);Refresh(); }
        public void SelectEquipmentPart(int part) { bool changed=selectedPart!=part;selectedPart=part;if(loadout[part]>=0)selected=loadout[part];Refresh();if(changed && character.TryGetComponent<UiPanelReveal>(out var reveal))reveal.RefreshContent(2); }
        public void OpenMap() => Show(map);
        public void OpenJournal() => Show(journal);
        public void OpenSettings()
        {
            Component<UnityEngine.UI.Slider>("Settings/Panel/Volume").SetValueWithoutNotify(AudioListener.volume);
            Show(settings);
        }
        public void SetVolume(float value) => AudioListener.volume=value;
        public void SetFilter(int value) { bool changed=filter!=value || crafting;equipmentMode=false;filter=value; crafting=false; Refresh(); ResetInventoryScroll();if(changed && inventory.TryGetComponent<UiPanelReveal>(out var reveal)){reveal.RefreshContent(1);reveal.RefreshContent(2);} }
        public void Select(int index) { bool changed=selected!=index;selected=index;if(equipmentMode)selectedPart=(int)items[index].equipmentPart; Refresh();var panel=equipmentMode?character:inventory;if(changed && panel.TryGetComponent<UiPanelReveal>(out var reveal))reveal.RefreshContent(2); }
        public void ShowQuickTooltip(int index)
        {
            var item=items[quickItems[index]];
            var tip=transform.Find("HUD/QuickTooltip");
            tip.gameObject.SetActive(true);
            Component<TMP_Text>("HUD/QuickTooltip/Label").text=item.title+"   /   "+item.quantity+"개";
        }
        public void HideQuickTooltip() => transform.Find("HUD/QuickTooltip").gameObject.SetActive(false);
        private void ResetInventoryScroll()
        {
            var scroll=Component<UnityEngine.UI.ScrollRect>("Inventory/Grid");
            scroll.StopMovement(); scroll.verticalNormalizedPosition=1;
        }
        public void Sort() => sortMenu.Toggle();
        public void SetSort(int order) { sortOrder=order; Refresh(); ResetInventoryScroll(); }

        public void UseQuick(int index)
        {
            equipmentMode=false;crafting=false;
            quickSelected=index; selected=quickItems[index];
            if(selected==2 || selected==3 || selected==7) UseSelected();
            else { if(items[selected].equipmentPart==EquipmentPart.Weapon) Equip(selected);  }
            Refresh();
            MarkItemChanged();
        }

        private void MarkItemChanged()
        {
            foreach(var slot in slots) slot.PulseItem(selected);
            foreach(var slot in quickSlots) slot.PulseItem(Array.IndexOf(quickItems,selected));
            if(equipmentMode) equipmentPanel.PulsePart(selectedPart);
        }

        private void ShowCondition(string reason)
        {
            Refresh();
            if(inventory.activeSelf || character.activeSelf)
                count.text += "\n<color=#AE5147>" + reason + "</color>";
            else { healthMotion.Pulse(); foodMotion.Pulse(); }
        }
        public void UseSelected()
        {
            if(craftResult.IsOpen) return;
            if (world)
            {
                var command = new DoHwa.World.WorldCommand { item = selected };
                if (crafting) { command.action = DoHwa.World.WorldAction.Craft; command.target = Array.Find(world.Catalog.recipes, r => r.result.item == selected).id; }
                else if (items[selected].equipmentPart != EquipmentPart.None)
                { command.action = DoHwa.World.WorldAction.Equip; command.part = (int)items[selected].equipmentPart; }
                else command.action = DoHwa.World.WorldAction.Consume;
                world.Submit(command);
                return;
            }
            if(equipmentMode && selected==loadout[selectedPart]) { loadout[selectedPart]=-1;Refresh();MarkItemChanged();return; }
            if(crafting)
            {
                if(selected==3 && items[6].quantity>=3 && items[2].quantity>=1) { items[6].quantity-=3; items[2].quantity--; items[3].quantity++; craftResult.Show(items[3].icon,items[3].title,1); }
                else if(selected==11 && items[5].quantity>=3) { items[5].quantity-=3; items[11].quantity++; craftResult.Show(items[11].icon,items[11].title,1); }
                else { Refresh(); ShowCondition("재료가 부족합니다"); return; }
            }
            else if(items[selected].quantity>0)
            {
                if(selected==2 || selected==7) { if(food>=100) { ShowCondition("포만감이 가득 찼습니다"); return; } items[selected].quantity--; food=Mathf.Min(100,food+20); health=Mathf.Min(100,health+5); foodMotion.Pulse();healthMotion.Pulse(); }
                else if(selected==3) { if(health>=100) { ShowCondition("체력이 가득 찼습니다"); return; } items[3].quantity--; health=Mathf.Min(100,health+35); healthMotion.Pulse(); }
                else if(items[selected].equipmentPart!=EquipmentPart.None) { Equip(selected);  }
            }
            Refresh();
            MarkItemChanged();
        }

        public void Notify(string message)
        {
            feedback.text=message; feedback.transform.parent.gameObject.SetActive(true); feedbackUntil=Time.unscaledTime+2.8f;
        }

        private bool Visible(int index)
        {
            if (crafting && world) return Array.Exists(world.Catalog.recipes, r => r.result.item == index);
            if(crafting) return index==3 || index==11;
            if(filter==0) return true;
            return filter==1 ? items[index].category=="장비" : filter==2 ? items[index].category=="소모품" : items[index].category=="재료";
        }

        public void Refresh()
        {
            title=Component<TMP_Text>("Inventory/Detail/Title");category=Component<TMP_Text>("Inventory/Detail/Category");description=Component<TMP_Text>("Inventory/Detail/Description");count=Component<TMP_Text>("Inventory/Detail/Count");actionLabel=Component<TMP_Text>("Inventory/Detail/Action/Label");action=Component<UnityEngine.UI.Button>("Inventory/Detail/Action");detailArt=Component<UnityEngine.UI.Image>("Inventory/Detail/Art");
            equipmentPanel.RefreshStats(health, food);
            var visible=Enumerable.Range(0,items.Length).Where(Visible).ToArray();
            visible = sortOrder switch
            {
                1 => visible.OrderBy(i=>items[i].category, StringComparer.CurrentCulture).ThenBy(i=>items[i].title, StringComparer.CurrentCulture).ToArray(),
                2 => visible.OrderBy(i=>items[i].title, StringComparer.CurrentCulture).ToArray(),
                3 => visible.OrderByDescending(i=>items[i].quantity).ThenBy(i=>i).ToArray(),
                _ => visible
            };
            if(!visible.Contains(selected)) selected=visible[0];
            for(int i=0;i<slots.Length;i++)
            {
                int id=i<visible.Length?visible[i]:-1;
                slots[i].Show(id,id>=0?items[id].icon:null,id>=0?items[id].quantity:0,id==selected,id>=0&&IsEquipped(id),!crafting&&filter==0);
                slots[i].transform.SetSiblingIndex(i);
            }
            for(int i=0;i<quickSlots.Length;i++) { int id=quickItems[i]; quickSlots[i].Show(i,items[id].icon,items[id].quantity,i==quickSelected,false); }
            var item=items[selected]; title.text=item.title; category.text=crafting?"제작법":item.category;
            detailArt.sprite=item.icon; detailArt.preserveAspect=true;
            description.text=item.description;
            count.text=crafting ? selected==3?$"필요 재료\n\n약초<pos=240>{items[6].quantity} / 3\n복숭아<pos=240>{items[2].quantity} / 1":$"필요 재료\n\n철광석<pos=240>{items[5].quantity} / 3" : $"보유 수량<pos=240>{item.quantity}";
            if (crafting && world)
            {
                var recipe = Array.Find(world.Catalog.recipes, r => r.result.item == selected);
                count.text = string.Join("\n", recipe.ingredients.Select(cost => items[cost.item].title + "<pos=240>" + items[cost.item].quantity + " / " + cost.amount));
            }
            actionLabel.text=crafting?"제작하기":item.category=="장비"?(selected==equipped?"장착 중":"장착하기"):item.category=="소모품"?"사용하기":"제작 재료";
            action.interactable=crafting ? selected==3?items[6].quantity>=3&&items[2].quantity>=1:items[5].quantity>=3 : item.quantity>0 && item.category!="재료" && selected!=equipped;
            if (crafting && world) action.interactable = Array.Find(world.Catalog.recipes, r => r.result.item == selected).ingredients.All(cost => items[cost.item].quantity >= cost.amount);
            if(crafting && !action.interactable) actionLabel.text="재료 부족";
            Component<TMP_Text>("Inventory/Header/Title").text=crafting?"제작":"배낭";
            Component<TMP_Text>("Inventory/InventoryHeading").text=crafting?"제작법":"소지품";
            Component<TMP_Text>("Inventory/InventorySubheading").text=crafting?"숲에서 모은 재료로 여정을 준비하세요":"여정에서 모은 도구와 작은 발견들";
            for(int i=0;i<4;i++) {
                bool active=!equipmentMode&&!crafting&&filter==i;
                Component<UnityEngine.UI.Image>("Inventory/Filters/Filter"+i).color=active?new Color(.29f,.32f,.39f):Color.clear;
                transform.Find("Inventory/Filters/Filter"+i+"/SelectionLine").gameObject.SetActive(false);
                Component<TMP_Text>("Inventory/Filters/Filter"+i+"/Label").color=active?new Color(.97f,.96f,.93f):new Color(.94f,.96f,.98f);
                var glyph=transform.Find("Inventory/Filters/Filter"+i+"/Glyph");
                if(glyph && glyph.TryGetComponent<UnityEngine.UI.Image>(out var graphic)) graphic.color=active?new Color(.97f,.96f,.93f):new Color(.94f,.96f,.98f);
            }
            Component<UnityEngine.UI.Image>("Inventory/Filters/Craft").color=crafting?new Color(.29f,.32f,.39f):Color.clear;
            transform.Find("Inventory/Filters/Craft/SelectionLine").gameObject.SetActive(false);
            Component<TMP_Text>("Inventory/Filters/Craft/Label").color=crafting?new Color(.97f,.96f,.93f):new Color(.94f,.96f,.98f);
            Component<UnityEngine.UI.Image>("Inventory/Filters/Craft/Glyph").color=crafting?new Color(.97f,.96f,.93f):new Color(.94f,.96f,.98f);
            healthLabel.text=$"{health:0} / 100"; foodLabel.text=$"{food:0} / 100";
            healthMotion.SetValue(health/100); foodMotion.SetValue(food/100);
            Component<TMP_Text>("Inventory/Equipment/Equipped").text="무기\n"+(loadout[0]>=0?items[loadout[0]].title:"미착용");
            Component<UnityEngine.UI.Image>("Inventory/Equipment/Icon").sprite=loadout[0]>=0?items[loadout[0]].icon:null; Component<UnityEngine.UI.Image>("Inventory/Equipment/Icon").enabled=loadout[0]>=0;
            transform.Find("Inventory/Grid").gameObject.SetActive(!equipmentMode);
            transform.Find("Inventory/Sort").gameObject.SetActive(!equipmentMode);
            equipmentPanel.gameObject.SetActive(equipmentMode);
            transform.Find("Inventory/Equipment").gameObject.SetActive(false);
            Component<UnityEngine.UI.Image>("Inventory/EquipmentTab").color=equipmentMode?new Color(.29f,.32f,.39f):Color.clear;
            Component<TMP_Text>("Inventory/EquipmentTab/Label").color=equipmentMode?new Color(.97f,.96f,.93f):new Color(.94f,.96f,.98f);
            if(!crafting) {
                actionLabel.text=item.category=="장비"?(IsEquipped(selected)?"장착 중":"장착하기"):actionLabel.text;
                if(item.category=="장비") action.interactable=item.quantity>0&&!IsEquipped(selected);
            }
            if(equipmentMode) {
                equipmentPanel.RefreshView(selectedPart);
                Component<TMP_Text>("Inventory/Header/Title").text="장비";
                Component<TMP_Text>("Inventory/InventoryHeading").text="부위별 장비";
                Component<TMP_Text>("Inventory/InventorySubheading").text="부위를 선택하거나 아래 소지 장비를 선택하세요";
                int id=loadout[selectedPart];
                bool candidate=items[selected].equipmentPart==(EquipmentPart)selectedPart && selected!=id;
                if(!candidate && id<0) { title.text=EquipmentPanel.PartNames[selectedPart];category.text="장비 부위";description.text="착용한 장비가 없습니다.";count.text="";detailArt.enabled=false;actionLabel.text="장착할 장비 없음";action.interactable=false; }
                else { detailArt.enabled=true;category.text="장비 / "+EquipmentPanel.PartNames[selectedPart];actionLabel.text=IsEquipped(selected)?"장착 해제":"장착하기";action.interactable=item.quantity>0;count.text=$"보유 수량<pos=240>{item.quantity}"; }
            } else detailArt.enabled=true;
        }
    }
}
















