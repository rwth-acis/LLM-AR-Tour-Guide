using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace i5.LLM_AR_Tourguide.Prefab_Scripts
{
    public class SevenPointScale : MonoBehaviour
    {
        public TextMeshProUGUI text;

        public Toggle toggle1;
        public Toggle toggle2;
        public Toggle toggle3;
        public Toggle toggle4;
        public Toggle toggle5;
        public Toggle toggle6;
        public Toggle toggle7;

        public ToggleGroup toggleGroup;

        public string GetQuestion()
        {
            return text.text;
        }

        public int GetSelectedValue()
        {
            if (toggle1.isOn) return 1;
            if (toggle2.isOn) return 2;
            if (toggle3.isOn) return 3;
            if (toggle4.isOn) return 4;
            if (toggle5.isOn) return 5;
            if (toggle6.isOn) return 6;
            if (toggle7.isOn) return 7;

            return 0;
        }
    }
}