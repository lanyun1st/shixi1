using System;
using Portfolio.Domain;
using UnityEngine;

namespace Portfolio.Configuration
{
    public sealed class SkillCatalog : ISkillParameters
    {
        static SkillCatalog instance;
        public static SkillCatalog Instance=>instance??(instance=new SkillCatalog());
        readonly SkillDefinition[] definitions=new SkillDefinition[18];
        SkillCatalog()
        {
            foreach(var definition in Resources.LoadAll<SkillDefinition>("Skills"))
            {
                if(!definition.IsValid)throw new InvalidOperationException("Invalid skill definition: "+definition.name);
                int key=definition.id+(definition.shield?9:0);
                if(definitions[key]!=null)throw new InvalidOperationException("Duplicate skill: "+definition.name);
                definitions[key]=definition;
            }
            for(int i=0;i<definitions.Length;i++)if(definitions[i]==null)throw new InvalidOperationException("Missing skill definition slot "+i);
            for(int role=0;role<2;role++)
            {
                int reachable=0;
                for(int pass=0;pass<9;pass++)for(int id=0;id<9;id++)if(Get(id,role==1).Available(reachable))reachable|=1<<id;
                if(reachable!=511)throw new InvalidOperationException("Cyclic skill prerequisites for role "+role);
            }
        }
        public SkillDefinition Get(int id,bool shield=false)=>id<0 || id>=9?null:definitions[id+(shield?9:0)];
        public float Value(int id,SkillParameter parameter,float fallback)=>Get(id)?.Value(parameter,fallback)??fallback;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache(){instance=null;}
    }
}
