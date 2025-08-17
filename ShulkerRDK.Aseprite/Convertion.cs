using System.ComponentModel;
using AsepriteDotNet;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Aseprite.Types;
using AsepriteDotNet.IO;
using AsepriteDotNet.Processors;
using ShulkerRDK.Shared;

namespace ShulkerRDK.Aseprite;

public static class Convertion {
    public static string? Method(string[] args,LevitateExecutionContext ec) {
        ec.Logger.AddNode("&bAseprite");
        Convert(args,ec.Logger);
        return null;
    }

    [Description("转换 Aseprite 文件")]
    public static void Command(string[] args,ShulkerContext shulkerContext) {
        Convert(args,new ChainedTerminal("&bAseprite"));
    }
    
    static void Convert(string[] args, IChainedLikeTerminal? ct = null) {
        if (!Tools.CheckParamLength(args,1,ct)) return;
        string asePath = args[1];
        string imagePath;
        if (Directory.Exists(asePath)) {
            imagePath = asePath;
            if (args.Length > 2) {
                imagePath = args[2];
            }
            ct?.WriteLine($"&7正在转换&8[&7{asePath}&8]>[&7{imagePath}&8]");
            string[] aseFiles = Directory.GetFiles(asePath,"*.aseprite",SearchOption.AllDirectories);
            foreach (string file in aseFiles) {
                ct?.WriteLine($"&7正在转换&8[&7{Path.GetFileName(file)}&8]",Terminal.MessageType.Debug);
                string relativePath = Path.GetRelativePath(asePath,file);
                string pngRelativePath = Path.ChangeExtension(relativePath,".png");
                string destPath = Path.Combine(imagePath,pngRelativePath);
                string? destDirectory = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDirectory)) {
                    Directory.CreateDirectory(destDirectory!);
                }
                AsepriteFile ase = AsepriteFileLoader.FromFile(file);
                Exporter(ase, destDirectory!);
            }
        } else if (File.Exists(asePath)) {
            imagePath = Path.GetDirectoryName(asePath)!;
            if (args.Length > 2) {
                imagePath = args[2];
            }
            ct?.WriteLine($"&7正在转换&8[&7{asePath}&8]>[&7{imagePath}&8]");
            AsepriteFile ase = AsepriteFileLoader.FromFile(asePath);
            Exporter(ase, imagePath);
        } else {
            ct?.WriteLine("&c无效的路径",Terminal.MessageType.Error);
        }
    }
    
    static void Exporter(AsepriteFile aseFile, string dest) {
        Dictionary<string,Sprite> spd = TaggedProcessor(aseFile);
        foreach (KeyValuePair<string,Sprite> spi in spd) {
            Texture texture = spi.Value.Texture;
            PngWriter.SaveTo(Path.Combine(dest,$"{aseFile.Name}{spi.Key}.png"),texture.Size.Width,texture.Size.Height,texture.Pixels.ToArray());
        }
    }

    static Dictionary<string,Sprite> TaggedProcessor(AsepriteFile aseFile) {
        Dictionary<string,Dictionary<int,string>> layerMatrix = [];
        Dictionary<string,Sprite> sprites = [];

        int index = 0;
        bool isBaseDisabled = false;
        foreach (AsepriteLayer layer in aseFile.Layers) {
            if ((!layer.IsVisible) & (!layer.Name.Contains('#'))) continue;
            //if (layer.IsBackgroundLayer) continue;
            if (layer is AsepriteTilemapLayer) continue;
            if (layer.Name == "#disableBase") {
                isBaseDisabled = true;
                continue;
            }
            if (!layer.Name.Contains('#')) Includer("#noTag",index,layer.Name,layerMatrix);
            else {
                List<string> slices = layer.Name.Split('#').ToList();
                slices.Remove(slices.First());
                foreach (string tag in slices) {
                    Includer(tag,index,layer.Name,layerMatrix);
                }
            }
            index++;
        }

        if (!isBaseDisabled) {
            sprites.Add("",Mixer(layerMatrix,aseFile,"#noTag",""));
            layerMatrix.Remove("");
        }

        foreach (KeyValuePair<string,Dictionary<int,string>> table in
                 layerMatrix.Where(table => table.Key != "#noTag")) {
            sprites.Add(table.Key,Mixer(layerMatrix,aseFile,"#noTag",table.Key));
            layerMatrix.Remove(table.Key);
        }

        return sprites;
    }

    static Sprite Mixer(Dictionary<string,Dictionary<int,string>> layerMatrix,AsepriteFile aseFile,string keyBase,string keyAttributes) {
        Dictionary<int,string> baseOut = [];
        if (layerMatrix.TryGetValue(keyBase,out Dictionary<int,string>? baseLayers)) {
            baseOut = baseLayers;
        }
        if (layerMatrix.TryGetValue(keyAttributes,out Dictionary<int,string>? baseAttributes)) {
            baseOut = baseOut
                .Concat(baseAttributes)
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(group => group.Key,
                              group => group.Last().Value);
        }
        return SpriteProcessor.Process(aseFile,0,baseOut
                                           .OrderBy(kvp => kvp.Key)
                                           .Select(kvp => kvp.Value)
                                           .ToList());
    }

    static void Includer(string tag,int index,string content,Dictionary<string,Dictionary<int,string>> matrix) {
        if (matrix.TryGetValue(tag,out Dictionary<int,string>? table)) {
            table.Add(index,content);
        } else {
            matrix.Add(tag,new Dictionary<int,string>());
            matrix[tag].Add(index,content);
        }
    }
}