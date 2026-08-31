using System.Text;

namespace ThirdPerson.Development.Gm
{
    public static class GmResultTextFormatter
    {
        public static string Format(GmCommandResponse response, int maximumCharacters)
        {
            var text = new StringBuilder();
            Append(text, $"[{response.code}] {response.completedAtUtc}\n{response.message}\n", maximumCharacters);
            foreach (GmResultSection section in response.sections)
            {
                Append(text, $"{section.title}\n", maximumCharacters);
                foreach (GmResultField field in section.fields)
                {
                    Append(text, $"  {field.label}: {field.value}\n", maximumCharacters);
                    if (text.Length >= maximumCharacters)
                        break;
                }
                if (text.Length >= maximumCharacters)
                    break;
            }
            return text.ToString();
        }

        static void Append(StringBuilder target, string value, int maximumCharacters)
        {
            int remaining = maximumCharacters - target.Length;
            if (value.Length <= remaining)
            {
                target.Append(value);
                return;
            }
            const string suffix = "\n[显示已截断]";
            target.Append(value, 0, System.Math.Max(0, remaining - suffix.Length));
            if (remaining >= suffix.Length)
                target.Append(suffix);
            else if (remaining > 0)
                target.Append('…', remaining);
        }
    }
}
