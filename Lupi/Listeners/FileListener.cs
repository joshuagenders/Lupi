using Newtonsoft.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lupi.Configuration;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;

namespace Lupi.Listeners
{
    public class FileListener : ITestResultListener
    {
        private readonly Config _config;
        //Formatting implementation from http://james.newtonking.com/archive/2008/03/29/formatwith-2-0-string-formatting-with-named-variables
        private readonly Regex _formatRegex = new Regex(@"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>:[^}]+)?(?<end>\})+",
              RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public FileListener(Config config)
        {
            _config = config;
        }

        public async Task OnResult(TestResult[] results, CancellationToken ct)
        {
            var lines = string.IsNullOrWhiteSpace(_config.Listeners.File.Format)
                ? results.Select(JsonConvert.SerializeObject)
                : results.Select(l => FormatWith(_config.Listeners.File.Format, l));
            await System.IO.File.AppendAllLinesAsync(_config.Listeners.File.Path, lines, ct);
        }

        public string FormatWith(string format, object source)
        {
            var values = new List<object>();
            var rewrittenFormat = _formatRegex.Replace(format, m =>
            {
                Group startGroup = m.Groups["start"];
                Group propertyGroup = m.Groups["property"];
                Group formatGroup = m.Groups["format"];
                Group endGroup = m.Groups["end"];

                var valueProp = source.GetType().GetProperty(propertyGroup.Value, BindingFlags.IgnoreCase);
                values.Add(valueProp == null ? string.Empty : valueProp.GetValue(source));

                return new string('{', startGroup.Captures.Count) + (values.Count - 1) + formatGroup.Value
                  + new string('}', endGroup.Captures.Count);
            });
            return string.Format(rewrittenFormat, values.ToArray());
        }
    }
}
