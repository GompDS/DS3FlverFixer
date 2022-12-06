using DS3FlverFixer.Util;
using SoulsAssetPipeline.FLVERImporting;
using SoulsFormats;

namespace DS3FlverFixer;

internal static class Program
{
    public static void Main(string[] args)
    {
        if (!BND4.Is(args[0])) return;
        BND4 bnd = BND4.Read(args[0]);

        BinderFile? file = bnd.Files.FirstOrDefault(x => x.Name.Contains(".flver"));
        if (file == null)
        {
            throw new FileNotFoundException("Binder contains no flver!");
        }

        FLVER2 flver = FLVER2.Read(file.Bytes);
        flver.BufferLayouts = new List<FLVER2.BufferLayout>();
        flver.GXLists = new List<FLVER2.GXList>();
        
        string cwd = AppDomain.CurrentDomain.BaseDirectory;

        FLVER2MaterialInfoBank materialInfoBank = FLVER2MaterialInfoBank.ReadFromXML($"{cwd}Res\\BankDS3.xml");
        
        List<FLVER2.Material> distinctMaterials = flver.Materials.DistinctBy(x => x.MTD).ToList();
        foreach (var distinctMat in distinctMaterials)
        {
            FLVER2.GXList gxList = new();
            gxList.AddRange(materialInfoBank
                .GetDefaultGXItemsForMTD(Path.GetFileName(distinctMat.MTD).ToLower()));

            if (flver.IsNewGxList(gxList))
            {
                flver.GXLists.Add(gxList);
            }
			
            foreach (var mat in flver.Materials.Where(x => x.MTD.Equals(distinctMat.MTD)))
            {
                mat.GXIndex = flver.GXLists.Count - 1;
            }
        }
        
        foreach (FLVER2.Mesh? mesh in flver.Meshes)
        {
            FLVER2MaterialInfoBank.MaterialDef matDef = materialInfoBank.MaterialDefs.Values
                .First(x => x.MTD.Equals(
                    $"{Path.GetFileName(flver.Materials[mesh.MaterialIndex].MTD).ToLower()}"));

            List<FLVER2.BufferLayout> bufferLayouts = matDef.AcceptableVertexBufferDeclarations[0].Buffers;
            
            mesh.Vertices = mesh.Vertices.Select(x => x.Pad(bufferLayouts)).ToList();
            List<int> layoutIndices = flver.GetLayoutIndices(bufferLayouts);
            mesh.VertexBuffers = layoutIndices.Select(x => new FLVER2.VertexBuffer(x)).ToList();
        }
        
        file.Bytes = flver.Write();
        
        bnd.Write(args[0], DCX.Type.DCX_DFLT_10000_44_9);
    }
}