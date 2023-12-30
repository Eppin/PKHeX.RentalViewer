namespace PKHeX.RentalViewer;

using System;
using Core;

public class Plugin : IPlugin
{
    private const uint KRentalTeams = 0x19CB0339;
    private const int Length = 0x844;

    public ISaveFileProvider SaveFileEditor { get; private set; } = null!;
    public IPKMView PkmEditor { get; private set; } = null!;

    public string Name => nameof(RentalViewer);
    public int Priority => 1;

    public void Initialize(params object[] args)
    {
        SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider)!;

        if (SaveFileEditor.SAV is not SAV9SV save9)
            return;

        PkmEditor = (IPKMView)Array.Find(args, z => z is IPKMView)!;
    }

    public void NotifySaveLoaded()
    {
        if (SaveFileEditor.SAV is not SAV9SV save9)
            return;

        var data = save9.Blocks.GetBlock(KRentalTeams).Data;
        var sav = SaveFileEditor.SAV;

        if (MessageBox.Show("Do you want to extract rental teams? They'll be placed in B0S0.", "Rental viewer", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
            return;

        // Always place in box 0 and start a slot 0
        const int box = 0;
        var slot = 0;

        // Maximum of 5 rental teams
        for (var i = 0; i < 5; i++)
        {
            // Skip 0x30 bytes [header], which contains rental name (and code)
            var team = data
                .Skip(i * Length)
                .Take((i + 1) * Length)
                .Skip(0x30)
                .ToList();

            foreach (var pkm in ExtractTeam(team))
                sav.SetBoxSlotAtIndex(pkm, box, slot++);
        }

        SaveFileEditor.ReloadSlots();
    }

    public bool TryLoadFile(string filePath)
    {
        return false;
    }
        
    private static IEnumerable<PK9> ExtractTeam(IReadOnlyCollection<byte> data)
    {
        const int size = 0x158;

        for (var i = 0; i < 6; i++)
        {
            var partialData = data
                .Skip(i * size)
                .Take((i + 1) * size)
                .ToArray();

            var pk9 = new PK9(partialData);

            if (pk9.Valid && pk9.Species != (int)Species.None)
                yield return pk9;
        }
    }
}
