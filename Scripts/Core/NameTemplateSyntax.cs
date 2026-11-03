using System.Text.RegularExpressions;
using XRL;
using XRL.Names;

namespace Cleo.TerraFirma.Scripts
{
	[HasModSensitiveStaticCache]
	public static class Cleo_TerraFirma_NameTemplateSyntax
	{
		const string OURS = "Cleo_TerraFirma_";
		static readonly Regex Token = new Regex("=([A-Za-z]+)=");

		[PreGameCacheInit]
		public static void Fix()
		{
			bool stars = false, equals = false;
			foreach (var pair in NameStyles.NameStyleTable)
			{
				if (pair.Key.StartsWith(OURS) || pair.Value?.Templates == null) continue;
				foreach (var t in pair.Value.Templates)
				{
					if (t?.Name == null) continue;
					if (t.Name.Contains("*Name*")) stars = true;
					if (t.Name.Contains("=name=")) equals = true;
				}
			}
			if (!stars || equals) return;

			int changed = 0;
			foreach (var pair in NameStyles.NameStyleTable)
			{
				if (!pair.Key.StartsWith(OURS) || pair.Value?.Templates == null) continue;
				foreach (var t in pair.Value.Templates)
				{
					if (t?.Name == null || t.Name.IndexOf('=') < 0) continue;
					t.Name = Token.Replace(t.Name, m => m.Groups[1].Value == "name" ? "*Name*" : "*" + m.Groups[1].Value + "*");
					changed++;
				}
			}
			if (changed > 0)
				Helpers.VerifyLog("NAMING", $"engine fills *Name* templates: rewrote {changed} Terra Firma =x= templates");
		}
	}
}
