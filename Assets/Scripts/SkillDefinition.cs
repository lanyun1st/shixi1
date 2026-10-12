using System;
using System.Globalization;
using Portfolio.Domain;
using UnityEngine;

namespace Portfolio.Configuration
{
    [Serializable]
    public struct SkillValue
    {
        public SkillParameter parameter;
        public float value;
        public SkillValue(SkillParameter parameter,float value){this.parameter=parameter;this.value=value;}
    }
    [CreateAssetMenu(menuName="Portfolio/Skill Definition")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [Tooltip("Stable slot: do not change an existing asset's ID or role.")]
        [Range(0,8)] public int id;
        public bool shield;
        public string displayName;
        public Sprite icon;
        [TextArea(3,5)] public string description;
        [Tooltip("Prerequisite bit mask: slot N is 1 << N. Zero means no prerequisite.")]
        [Range(0,511)] public int requiredMask;
        [Tooltip("Currently used for ranged slots 2, 4, 6, 7, 8 only. Other effects remain in combat rules.")]
        public SkillValue[] values=Array.Empty<SkillValue>();
        public float Value(SkillParameter parameter,float fallback)
        {
            foreach(var item in values)if(item.parameter==parameter)return item.value;
            return fallback;
        }
        public bool Available(int owned)=>(owned&requiredMask)==requiredMask;
        public bool IsValid
        {
            get
            {
                if(id<0 || id>=9 || string.IsNullOrWhiteSpace(displayName) || icon==null || string.IsNullOrWhiteSpace(description) ||
                    requiredMask<0 || requiredMask>511 || (requiredMask&(1<<id))!=0 || values==null)return false;
                for(int i=0;i<values.Length;i++)
                {
                    var v=values[i];if(!Enum.IsDefined(typeof(SkillParameter),v.parameter) || float.IsNaN(v.value) || float.IsInfinity(v.value) || v.value<=0)return false;
                    if(shield || !(id==2 && v.parameter==SkillParameter.Penetration || id==4 && (v.parameter==SkillParameter.ProjectileCount || v.parameter==SkillParameter.ProjectileDamage) ||
                        id==6 && v.parameter==SkillParameter.DamageMultiplier || id==7 && v.parameter==SkillParameter.MovementMultiplier || id==8 && v.parameter==SkillParameter.DashCooldownMultiplier))return false;
                    if((v.parameter==SkillParameter.ProjectileCount || v.parameter==SkillParameter.Penetration) && v.value!=Mathf.Floor(v.value))return false;
                    for(int j=0;j<i;j++)if(values[j].parameter==v.parameter)return false;
                }
                return !Description.Contains("{");
            }
        }
        public string Description
        {
            get
            {
                string text=description;
                foreach(var item in values)
                {
                    text=text.Replace("{"+item.parameter+"}",item.value.ToString("0.##",CultureInfo.InvariantCulture));
                    float percent=item.parameter==SkillParameter.DashCooldownMultiplier?(1-item.value)*100:(item.value-1)*100;
                    text=text.Replace("{"+item.parameter+"Percent}",percent.ToString("0.##",CultureInfo.InvariantCulture));
                }
                return text;
            }
        }
    }
}
