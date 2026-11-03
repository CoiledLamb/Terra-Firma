using XRL.World;

namespace Cleo.TerraFirma.Scripts
{
	public static class FurnitureCellGuard
	{
		public static bool Block(AutoexploreObjectEvent E, GameObject Furniture)
		{
			if (E.Item != Furniture || Furniture.IsTakeable())
				return true;
			if (E.Command == "Autoget")
				E.Command = null;
			return false;
		}
	}
}
