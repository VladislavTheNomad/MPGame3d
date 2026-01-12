using UnityEngine;
using UnityEngine.UI;

namespace MPGame3d
{
    public class UIService : MonoBehaviour
    {
        [SerializeField] private Slider _hpSlider;
        [SerializeField] private Slider _expSlider;

        public void UpdateMaxHP(int value)
        {
            _hpSlider.maxValue = value;
        }
        
        public void UpdateHpBar(int currentHp)
        {
            _hpSlider.value = currentHp;
        }

        public void UpdateExpBar(int currentExp)
        {
            _expSlider.value = currentExp;
        }

        public void UpdateMaxExpToLvl(int playerExpToLevelUp)
        {
            _expSlider.maxValue = playerExpToLevelUp;
        }
    }
}
