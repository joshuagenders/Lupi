using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;

namespace Lupi.Listeners
{
    public static class StringExtensions
    {
        //Formatting implementation from http://james.newtonking.com/archive/2008/03/29/formatwith-2-0-string-formatting-with-named-variables
        private static readonly Regex _formatRegex = new Regex(@"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>[:,][^}]+)?(?<end>\})+",
              RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static string FormatWith(this string format, object source)
        {
            var values = new List<object>();
            var rewrittenFormat = _formatRegex.Replace(format, m =>
            {
                Group startGroup = m.Groups["start"];
                Group propertyGroup = m.Groups["property"];
                Group formatGroup = m.Groups["format"];
                Group endGroup = m.Groups["end"];

                var val = source.GetType().GetProperty(propertyGroup.Value)?.GetValue(source);
                val ??= source.GetType().GetField(propertyGroup.Value)?.GetValue(source);
                values.Add(val ?? string.Empty);

                return new string('{', startGroup.Captures.Count) + (values.Count - 1) + formatGroup.Value
                  + new string('}', endGroup.Captures.Count);
            });
            return string.Format(rewrittenFormat, values.ToArray());
        }
    }
}
