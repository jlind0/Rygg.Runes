
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Maui.Controls;

namespace RyggRunes.MAUI.Client.Converters
{
    public class PropertySetter : TriggerAction<RadButton>
    {
        public object Target { get; set; }
        public string Property { get; set; }
        public object Value { get; set; }
        protected override void Invoke(RadButton sender)
        {
            if (Target != null && !string.IsNullOrEmpty(Property))
            {
                // Check if the target control has the specified property
                var propertyInfo = Target.GetType().GetProperty(Property);
                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    // Set the property to the specified value
                    propertyInfo.SetValue(Target, Value);
                }
            }
        }
    }
}
