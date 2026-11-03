using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Kobold;

namespace Cleo.TerraFirma.Scripts
{
	public static class Cleo_TerraFirma_PedestalTile
	{
		public const string BAR = "figurine_pedestal.png";
		private const string PREFIX = "Cleo_TerraFirma_Pedestal:";

		private static readonly Dictionary<string, string> Painted = new Dictionary<string, string>();
		private static readonly HashSet<string> Failed = new HashSet<string>();

		public static string Get(string Tile)
		{
			if (Tile.IsNullOrEmpty() || Failed.Contains(Tile))
				return null;
			if (Painted.TryGetValue(Tile, out string have) && SpriteManager.HasTextureInfo(have))
				return have;
			string key = null;
			try
			{
				if (GameManager.IsOnUIContext())
					key = Paint(Tile);
				else
					GameManager.Instance.uiQueue.awaitTask(() => key = Paint(Tile));
			}
			catch (Exception e)
			{
				Helpers.VerifyLog("PEDESTAL", $"painting {Tile} failed: {e.GetType().Name}: {e.Message}");
				key = null;
			}
			if (key == null)
				Failed.Add(Tile);
			else
				Painted[Tile] = key;
			return key;
		}

		private static string Paint(string Tile)
		{
			exTextureInfo src = SpriteManager.GetTextureInfo(Tile, false);
			exTextureInfo bar = SpriteManager.GetTextureInfo(BAR, false);
			if (src?.texture == null || bar?.texture == null || src.rotated || bar.rotated || src.trim)
				return null;
			int w = src.width, h = src.height;
			if (bar.width != w || bar.height != h)
				return null;
			UnityEngine.Color[] creature = src.texture.GetPixels(src.x, src.y, w, h, 0);
			UnityEngine.Color[] pedestal = bar.texture.GetPixels(bar.x, bar.y, w, h, 0);

			int low = h, high = -1, barTop = -1;
			for (int r = 0; r < h; r++)
			{
				for (int c = 0; c < w; c++)
				{
					if (creature[r * w + c].a > 0)
					{
						low = Math.Min(low, r);
						high = Math.Max(high, r);
					}
					if (pedestal[r * w + c].a > 0)
						barTop = Math.Max(barTop, r);
				}
			}
			if (high < 0 || barTop < 0)
				return null;
			int headroom = h - 1 - high;
			int lift = Math.Max(0, barTop + 2 - low);
			if (lift > headroom)
				lift = Math.Max(0, barTop + 1 - low);
			lift = Math.Min(lift, headroom);

			UnityEngine.Color[] outPixels = (UnityEngine.Color[])pedestal.Clone();
			for (int r = 0; r + lift < h; r++)
			{
				for (int c = 0; c < w; c++)
				{
					UnityEngine.Color px = creature[r * w + c];
					if (px.a > 0)
						outPixels[(r + lift) * w + c] = px;
				}
			}

			UnityEngine.Texture2D tex = new UnityEngine.Texture2D(w, h, UnityEngine.TextureFormat.ARGB32, false);
			tex.filterMode = UnityEngine.FilterMode.Point;
			tex.SetPixels(outPixels);
			tex.Apply();
			exTextureInfo info = UnityEngine.ScriptableObject.CreateInstance<exTextureInfo>();
			info.name = PREFIX + Tile;
			info.texture = tex;
			info.width = info.rawWidth = w;
			info.height = info.rawHeight = h;
			info.x = 0;
			info.y = 0;
			info.ShaderMode = src.ShaderMode;

			string key = PREFIX + Tile;
			if (!Register(key, info))
				return null;
			Helpers.VerifyLog("PEDESTAL", $"painted {Tile}: lift {lift}");
			return key;
		}

		private static bool Register(string Key, exTextureInfo Info)
		{
			const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
			IDictionary<string, exTextureInfo> paths = typeof(SpriteManager).GetField("PathMap", flags)?.GetValue(null) as IDictionary<string, exTextureInfo>;
			ReaderWriterLockSlim gate = typeof(SpriteManager).GetField("InfoLock", flags)?.GetValue(null) as ReaderWriterLockSlim;
			if (paths == null)
				return false;
			gate?.EnterWriteLock();
			try
			{
				paths[Key] = Info;
			}
			finally
			{
				gate?.ExitWriteLock();
			}
			return SpriteManager.HasTextureInfo(Key);
		}
	}
}
