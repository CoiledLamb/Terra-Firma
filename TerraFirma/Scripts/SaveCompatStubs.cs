using System;

namespace XRL.World.Parts
{
	// Save-compat stub for Cleo_TerraFirma_FallenRockPart, whose class moved to quarantine/
	// (2026-09-30). 0.1.2.5 Fissure dropped pebbles carrying it, so real saves hold it.
	//
	// Engine facts (decompiled 2026-09-30): IPart.Load reads each part as a length-prefixed block.
	// With no class by the saved name, SkipBlock skips the bytes, bumps Reader.Errors, and XRLGame
	// shows Popup.DisplayLoadError. The default Read consumes one value per public field in order,
	// so a class with fewer fields would leave the stream mid-block. The stub refuses its saved
	// fields and seeks past the block in ReadError, which IPart.Load consults before SkipBlock and
	// which counts no error. It then sits inert on the stone; the next save writes it empty.
	//
	// Restoring the class means deleting this stub.
	public class Cleo_TerraFirma_FallenRockPart : IPart
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
