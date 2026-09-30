using System;

namespace XRL.World.Parts
{
	// Save-compat stubs for parts whose classes moved to quarantine/ (2026-09-30). A 0.1.2.5 save
	// can hold these parts on objects that spawned while they shipped.
	//
	// Engine facts (decompiled 2026-09-30): IPart.Load reads each part as a length-prefixed block.
	// With no class by the saved name, ReadTokenizedType throws, SkipBlock skips the bytes and bumps
	// Reader.Errors, and XRLGame shows Popup.DisplayLoadError. An empty class is worse: the default
	// Read consumes one value per field, so zero fields would leave the stream mid-block. Each stub
	// therefore refuses its saved fields and seeks past the block in ReadError, which IPart.Load
	// consults before SkipBlock and which counts no error. The stub then sits inert on the object,
	// and the next save writes it with no fields.
	//
	// Restoring a quarantined class means deleting its stub here.

	public class Cleo_TerraFirma_TimedForm : IPart
	{
		public override void Read(GameObject Basis, SerializationReader Reader)
		{
			throw new NotSupportedException("quarantined part; saved data skipped");
		}

		public override bool ReadError(Exception Exception, SerializationReader Reader, long Start, int Length)
		{
			Reader.Stream.Position = Start + Length;
			return true;
		}
	}

	public class Cleo_TerraFirma_FungalLottery : IPart
	{
		public override void Read(GameObject Basis, SerializationReader Reader)
		{
			throw new NotSupportedException("quarantined part; saved data skipped");
		}

		public override bool ReadError(Exception Exception, SerializationReader Reader, long Start, int Length)
		{
			Reader.Stream.Position = Start + Length;
			return true;
		}
	}
}
