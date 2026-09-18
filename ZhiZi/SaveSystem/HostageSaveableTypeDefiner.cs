using System.Collections.Generic;
using TaleWorlds.SaveSystem;
using ZhiZi.Data;

namespace ZhiZi.SaveSystem
{
    public sealed class HostageSaveableTypeDefiner : SaveableTypeDefiner
    {
        public HostageSaveableTypeDefiner() : base(9152200)
        {
        }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(HostageContract), 1);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(List<HostageContract>));
        }
    }
}
