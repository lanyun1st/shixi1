using UnityEngine;
using Portfolio.Configuration;

namespace Portfolio.UI
{
    public static class SkillIconCatalog
    {
        public static Sprite Get(int id,bool shield=false)=>SkillCatalog.Instance.Get(id,shield)?.icon;
        public static Sprite Dash(bool shield)=>Get(shield?5:7,shield);
    }
}
