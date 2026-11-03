using System.Reflection;
using XRL;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

namespace Cleo.TerraFirma.Scripts
{
	public static class Compat
	{
		static readonly FieldInfo MutationEntryField = typeof(BaseMutation).GetField("Entry", BindingFlags.Public | BindingFlags.Instance);
		static readonly FieldInfo MutationEntryNameField = typeof(BaseMutation).GetField("EntryName", BindingFlags.Public | BindingFlags.Instance);

		public static bool AddMutation(Mutations Mutations, string NameOrClass, int Level = 1)
		{
			if (Mutations == null || NameOrClass == null)
				return false;
			if (!MutationFactory.TryGetMutationEntry(NameOrClass, out MutationEntry entry))
			{
				MetricsManager.LogError("Terra Firma: no mutation entry for '" + NameOrClass + "' on " + Mutations.ParentObject?.DebugName);
				return false;
			}
			Mutations.AddMutation(entry, Level);
			return true;
		}

		public static void AddMutation(Mutations Mutations, MutationEntry Entry, int Level = 1)
		{
			if (Mutations != null && Entry != null)
				Mutations.AddMutation(Entry, Level);
		}

		public static void AddMutation(Mutations Mutations, BaseMutation Mutation, int Level = 1)
		{
			if (Mutations == null || Mutation == null)
				return;
			if (MutationEntryField != null && MutationEntryField.GetValue(Mutation) == null
				&& MutationFactory.TryGetMutationEntry(Mutation.GetType().Name, out MutationEntry entry))
			{
				MutationEntryField.SetValue(Mutation, entry);
				MutationEntryNameField?.SetValue(Mutation, entry.Name);
			}
			Mutations.AddMutation(Mutation, Level);
		}

		public static readonly int SaveFileVersion =
			typeof(SerializationWriter).GetMethod("WriteFormatVersions", BindingFlags.Public | BindingFlags.Instance) != null ? 500 : 409;
	}
}
