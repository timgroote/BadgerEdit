using System;
using System.Collections.Generic;
using System.Linq;

namespace TGREdit
{
    public class LanguageDefinition
    {
        Dictionary<string, PaletteIndex> TokenRegexStrings;

        public string Name;

        public List<String> Keywords; //<-- todo : keywords?

        public Dictionary<string, PaletteIndex> tokenRegexStrings;

        public List<string> Identifiers;
        public string CommentStart, CommentEnd;
        public bool CaseSensitive;


        public LanguageDefinition(string name, string[] keywords, string[] identifiers, Dictionary<string, PaletteIndex> regexStrings, string commentStart, string commentEnd, bool caseSensitive)
        {
            Name = name;
            Keywords = keywords.ToList();

            Identifiers = identifiers.ToList();

            tokenRegexStrings = regexStrings;
            CommentStart = commentStart;
            CommentEnd = commentEnd;

            CaseSensitive = caseSensitive;
        }

        //tgr : not bothering with anything that is not a shader language for the time being.

        //public static LanguageDefinition CPlusPlus = new LanguageDefinition();

        public static LanguageDefinition HLSL = new LanguageDefinition("HLSL", new[]
        {
            "AppendStructuredBuffer", "asm", "asm_fragment", "BlendState", "bool", "break", "Buffer", "ByteAddressBuffer", "case", "cbuffer", "centroid", "class", "column_major", "compile", "compile_fragment",
            "CompileShader", "const", "continue", "ComputeShader", "ConsumeStructuredBuffer", "default", "DepthStencilState", "DepthStencilView", "discard", "do", "double", "DomainShader", "dword", "else",
            "export", "extern", "false", "float", "for", "fxgroup", "GeometryShader", "groupshared", "half", "Hullshader", "if", "in", "inline", "inout", "InputPatch", "int", "interface", "line", "lineadj",
            "linear", "LineStream", "matrix", "min16float", "min10float", "min16int", "min12int", "min16uint", "namespace", "nointerpolation", "noperspective", "NULL", "out", "OutputPatch", "packoffset",
            "pass", "pixelfragment", "PixelShader", "point", "PointStream", "precise", "RasterizerState", "RenderTargetView", "return", "register", "row_major", "RWBuffer", "RWByteAddressBuffer", "RWStructuredBuffer",
            "RWTexture1D", "RWTexture1DArray", "RWTexture2D", "RWTexture2DArray", "RWTexture3D", "sample", "sampler", "SamplerState", "SamplerComparisonState", "shared", "snorm", "stateblock", "stateblock_state",
            "static", "string", "struct", "switch", "StructuredBuffer", "tbuffer", "technique", "technique10", "technique11", "texture", "Texture1D", "Texture1DArray", "Texture2D", "Texture2DArray", "Texture2DMS",
            "Texture2DMSArray", "Texture3D", "TextureCube", "TextureCubeArray", "true", "typedef", "triangle", "triangleadj", "TriangleStream", "uint", "uniform", "unorm", "unsigned", "vector", "vertexfragment",
            "VertexShader", "void", "volatile", "while",
            "bool1","bool2","bool3","bool4","double1","double2","double3","double4", "float1", "float2", "float3", "float4", "int1", "int2", "int3", "int4", "in", "out", "inout",
            "uint1", "uint2", "uint3", "uint4", "dword1", "dword2", "dword3", "dword4", "half1", "half2", "half3", "half4",
            "float1x1","float2x1","float3x1","float4x1","float1x2","float2x2","float3x2","float4x2",
            "float1x3","float2x3","float3x3","float4x3","float1x4","float2x4","float3x4","float4x4",
            "half1x1","half2x1","half3x1","half4x1","half1x2","half2x2","half3x2","half4x2",
            "half1x3","half2x3","half3x3","half4x3","half1x4","half2x4","half3x4","half4x4"
        },
        new[]{"abort", "abs", "acos", "all", "AllMemoryBarrier", "AllMemoryBarrierWithGroupSync", "any", "asdouble", "asfloat", "asin", "asint", "asint", "asuint",
            "asuint", "atan", "atan2", "ceil", "CheckAccessFullyMapped", "clamp", "clip", "cos", "cosh", "countbits", "cross", "D3DCOLORtoUBYTE4", "ddx",
            "ddx_coarse", "ddx_fine", "ddy", "ddy_coarse", "ddy_fine", "degrees", "determinant", "DeviceMemoryBarrier", "DeviceMemoryBarrierWithGroupSync",
            "distance", "dot", "dst", "errorf", "EvaluateAttributeAtCentroid", "EvaluateAttributeAtSample", "EvaluateAttributeSnapped", "exp", "exp2",
            "f16tof32", "f32tof16", "faceforward", "firstbithigh", "firstbitlow", "floor", "fma", "fmod", "frac", "frexp", "fwidth", "GetRenderTargetSampleCount",
            "GetRenderTargetSamplePosition", "GroupMemoryBarrier", "GroupMemoryBarrierWithGroupSync", "InterlockedAdd", "InterlockedAnd", "InterlockedCompareExchange",
            "InterlockedCompareStore", "InterlockedExchange", "InterlockedMax", "InterlockedMin", "InterlockedOr", "InterlockedXor", "isfinite", "isinf", "isnan",
            "ldexp", "length", "lerp", "lit", "log", "log10", "log2", "mad", "max", "min", "modf", "msad4", "mul", "noise", "normalize", "pow", "printf",
            "Process2DQuadTessFactorsAvg", "Process2DQuadTessFactorsMax", "Process2DQuadTessFactorsMin", "ProcessIsolineTessFactors", "ProcessQuadTessFactorsAvg",
            "ProcessQuadTessFactorsMax", "ProcessQuadTessFactorsMin", "ProcessTriTessFactorsAvg", "ProcessTriTessFactorsMax", "ProcessTriTessFactorsMin",
            "radians", "rcp", "reflect", "refract", "reversebits", "round", "rsqrt", "saturate", "sign", "sin", "sincos", "sinh", "smoothstep", "sqrt", "step",
            "tan", "tanh", "tex1D", "tex1D", "tex1Dbias", "tex1Dgrad", "tex1Dlod", "tex1Dproj", "tex2D", "tex2D", "tex2Dbias", "tex2Dgrad", "tex2Dlod", "tex2Dproj",
            "tex3D", "tex3D", "tex3Dbias", "tex3Dgrad", "tex3Dlod", "tex3Dproj", "texCUBE", "texCUBE", "texCUBEbias", "texCUBEgrad", "texCUBElod", "texCUBEproj", "transpose", "trunc"
        }, new Dictionary<string, PaletteIndex>()
            {
                {"//.*", PaletteIndex.Comment },
                {"[ \\t]*#[ \\t]*[a-zA-Z_]+", PaletteIndex.Preprocessor },
                { "L?\\\"(\\\\.|[^\\\"])*\\\"", PaletteIndex.String},
                {"\\'\\\\?[^\\']\\'", PaletteIndex.CharLiteral},
                {"[+-]?([0-9]+([.][0-9]*)?|[.][0-9]+)([eE][+-]?[0-9]+)?[fF]?", PaletteIndex.Number},
                {"[+-]?[0-9]+[Uu]?[lL]?[lL]?", PaletteIndex.Number},
                {"0[0-7]+[Uu]?[lL]?[lL]?", PaletteIndex.Number},
                {"0[xX][0-9a-fA-F]+[uU]?[lL]?[lL]?", PaletteIndex.Number},
                {"[a-zA-Z_][a-zA-Z0-9_]*", PaletteIndex.Identifier},
                {"[\\[\\]\\{\\}\\!\\%\\^\\&\\*\\(\\)\\-\\+\\=\\~\\|\\<\\>\\?\\/\\;\\,\\.]", PaletteIndex.Punctuation},
            }, 
            "/*", 
            "*/", 
            true    
        );

        public static LanguageDefinition GLSL = new LanguageDefinition(
            "GLSL", 
            new []{"auto", "break", "case", "char", "const", "continue", "default", "do", "double", "else", "enum", "extern", "float", "for", "goto", "if", "inline", "int", "long", "register", "restrict", "return", "short",
                "signed", "sizeof", "static", "struct", "switch", "typedef", "union", "unsigned", "void", "volatile", "while", "_Alignas", "_Alignof", "_Atomic", "_Bool", "_Complex", "_Generic", "_Imaginary",
                "_Noreturn", "_Static_assert", "_Thread_local"}, 
            new []{ "abort", "abs", "acos", "asin", "atan", "atexit", "atof", "atoi", "atol", "ceil", "clock", "cosh", "ctime", "div", "exit", "fabs", "floor", "fmod", "getchar", "getenv", "isalnum", "isalpha", "isdigit", "isgraph",
                "ispunct", "isspace", "isupper", "kbhit", "log10", "log2", "log", "memcmp", "modf", "pow", "putchar", "putenv", "puts", "rand", "remove", "rename", "sinh", "sqrt", "srand", "strcat", "strcmp", "strerror", "time", "tolower", "toupper"
            }, 
            new Dictionary<string, PaletteIndex>()
            {
                { "//*", PaletteIndex.Comment},
                { "[ \\t]*#[ \\t]*[a-zA-Z_]+", PaletteIndex.Preprocessor},
                { "L?\\\"(\\\\.|[^\\\"])*\\\"", PaletteIndex.String},
                { "\\'\\\\?[^\\']\\'", PaletteIndex.CharLiteral},
                { "[+-]?([0-9]+([.][0-9]*)?|[.][0-9]+)([eE][+-]?[0-9]+)?[fF]?", PaletteIndex. Number},
                { "[+-]?[0-9]+[Uu]?[lL]?[lL]?", PaletteIndex.Number},
                { "0[0-7]+[Uu]?[lL]?[lL]?", PaletteIndex.Number},
                { "0[xX][0-9a-fA-F]+[uU]?[lL]?[lL]?", PaletteIndex.Number},
                { "[a-zA-Z_][a-zA-Z0-9_]*", PaletteIndex.Identifier},
                { "[\\[\\]\\{\\}\\!\\%\\^\\&\\*\\(\\)\\-\\+\\=\\~\\|\\<\\>\\?\\/\\;\\,\\.]", PaletteIndex.Punctuation}

            }, 
            "/*", 
            "*/", 
            true
        );
       // public static LanguageDefinition C = new LanguageDefinition();
       // public static LanguageDefinition AngelScript = new LanguageDefinition();
       // public static LanguageDefinition Lua = new LanguageDefinition();

    }
}