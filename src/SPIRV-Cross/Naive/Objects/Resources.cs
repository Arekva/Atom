using System;
using System.Collections.Generic;
using SPIRVCross.Base;
using static SPIRVCross.Base.SPIRV;

namespace SPIRVCross.Naive
{
    public class Resources : ISpvcObject<spvc_resources>
    {
        public nint Handle { get; set; }

        public ReflectedResource[] GetResourceListForType(ResourceType type)
        {
            nuint count;
            Span<spvc_reflected_resource> nativeResources;
            
            unsafe
            {
                spvc_reflected_resource* list = null;
                spvc_resources_get_resource_list_for_type(this.ToSpvc(), (spvc_resource_type) type, (spvc_reflected_resource*)&list, &count);
                nativeResources = new(list, *(i32*)&count);
            }

            ReflectedResource[] resources = new ReflectedResource[count];
            for (i32 i = 0; i < (i32)count; i++)
            {
                unsafe
                {
                    resources[i] = new()
                    {
                        Name = LowLevel.GetString(nativeResources[i].name),
                        BaseTypeId = nativeResources[i].base_type_id,
                        Id = nativeResources[i].id,
                        TypeId = nativeResources[i].type_id
                    };
                }
            }

            return resources;
        }
    }
}