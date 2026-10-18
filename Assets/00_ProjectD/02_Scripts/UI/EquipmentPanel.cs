using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace DoHwa.UI
{
    public enum EquipmentPart { None=-1, Weapon, Head, Body, Legs, Feet, Necklace, Ring, Earrings, Bag }

    public sealed class EquipmentPanel : MonoBehaviour
    {
        public static readonly string[] PartNames={"무기","모자","상의","하의","신발","목걸이","반지","귀걸이","가방"};
        [SerializeField] private RpgUiController owner;
        [SerializeField] private Image[] faces, icons;
        [SerializeField] private TMP_Text[] names;
        
        [SerializeField] private TMP_Text[] statValues;
        public void ConfigureStats(TMP_Text[] values) => statValues=values;
        public void RefreshStats(float health, float food) { statValues[0].text=$"{health:0}<size=16> / 100</size>";statValues[1].text=$"{food:0}<size=16> / 100</size>"; }
        public void Configure(RpgUiController controller,Image[] backgrounds,Image[] art,TMP_Text[] titles)
        { owner=controller;faces=backgrounds;icons=art;names=titles; }
        public void SelectPart(int part) => owner.SelectEquipmentPart(part);
        public void PulsePart(int part) { faces[part].TryGetComponent<EquipmentSlotFeedback>(out var feedback); feedback.Pulse(); }
        public void RefreshView(int selected)
        {
            
            for(int i=0;i<faces.Length;i++) {
                int id=owner.EquippedAt(i);bool filled=id>=0;
                icons[i].enabled=filled;icons[i].sprite=filled?owner.Catalog[id].icon:null;
                names[i].text=filled?owner.Catalog[id].title:"미착용";
                faces[i].TryGetComponent<EquipmentSlotFeedback>(out var feedback);
                feedback.SetState(i==selected,filled);
                
            }
            
        }
    }
}



