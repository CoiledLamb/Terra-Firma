using ConsoleLib.Console;
using Kobold;
using UnityEngine;
using XRL.World;

namespace Cleo.TerraFirma.Scripts
{
	public class Cleo_TerraFirma_JuiceShiftStep : CombatJuiceEntry
	{
		private IRenderable Tile;
		private ex3DSprite2 Target;
		private Vector3 Center;
		private float Start;
		private float Beat;
		private bool Rising;
		private Vector3 OrigScale;
		private bool ScaleFallback;
		private exTextureInfo BaseInfo;
		private exTextureInfo[] SubInfos;
		private int Applied = -1;
		private Color Foreground;
		private Color Background;
		private Color Detail;
		private Color DetailNudge;

		public Cleo_TerraFirma_JuiceShiftStep(IRenderable tile, Vector3 center, float start, float beat, bool rising)
		{
			Tile = tile;
			Center = center;
			Start = start;
			Beat = Mathf.Max(0.01f, beat);
			Rising = rising;
			duration = start + 3f * this.Beat + 0.02f;
		}

		public override void start()
		{
			Target = SpriteManager.GetPooledSprite(Tile, Transparent: false);
			Foreground = Target.color;
			Background = Target.backcolor;
			Detail = Target.detailcolor;
			DetailNudge = new Color(Detail.r, Detail.g, Detail.b, 0.996f);
			Transform transform = Target.gameObject.transform;
			transform.SetParent(GameManager.Instance.TileRoot.transform);
			OrigScale = transform.localScale;
			ScaleFallback = false;
			BaseInfo = Target.textureInfo;
			SubInfos = null;
			if (BaseInfo != null && !BaseInfo.rotated && !BaseInfo.isDiced)
			{
				SubInfos = new exTextureInfo[2];
				SubInfos[0] = Cleo_TerraFirma_StoneRiseFX.CloneSubRect(BaseInfo, 0f, 2f / 3f, 1f, 1f / 3f);
				SubInfos[1] = Cleo_TerraFirma_StoneRiseFX.CloneSubRect(BaseInfo, 0f, 1f / 3f, 1f, 2f / 3f);
			}
			transform.position = Center + new Vector3(0f, 0f, Rising ? 100f : -10f);
		}

		private int Thirds()
		{
			int k = t < Start ? -1 : (int)((t - Start) / Beat);
			if (Rising)
				return k < 0 || k > 2 ? 0 : k + 1;
			return k < 0 ? 3 : (k > 1 ? 0 : 2 - k);
		}

		public override void update()
		{
			if (Target == null)
				return;
			Target.color = Foreground;
			Target.backcolor = Background;
			Target.detailcolor = DetailNudge;
			Target.detailcolor = Detail;
			int thirds = Thirds();
			if (thirds == 0)
			{
				Target.transform.position = Center + new Vector3(0f, 0f, 100f);
				return;
			}
			if (thirds != Applied)
			{
				Applied = thirds;
				if (SubInfos != null)
					Target.textureInfo = thirds == 3 ? BaseInfo : SubInfos[thirds - 1];
				else
				{
					Target.gameObject.transform.localScale = Vector3.Scale(OrigScale, new Vector3(1f, thirds / 3f, 1f));
					ScaleFallback = true;
				}
			}
			float sy = thirds / 3f;
			Target.transform.position = Center + new Vector3(0f, -12f * (1f - sy), -10f);
		}

		public override void finish()
		{
			if (Target != null)
			{
				if (ScaleFallback)
					Target.gameObject.transform.localScale = OrigScale;
				if (SubInfos != null && BaseInfo != null)
					Target.textureInfo = BaseInfo;
				SpriteManager.Return(Target);
				Target = null;
			}
			if (SubInfos != null)
			{
				for (int i = 0; i < SubInfos.Length; i++)
				{
					if (SubInfos[i] != null)
						UnityEngine.Object.Destroy(SubInfos[i]);
				}
				SubInfos = null;
			}
			base.finish();
		}
	}
}
