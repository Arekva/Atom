using SPIRVCross.Naive;

byte[] data = File.ReadAllBytes("files/test.spv");

using Context spirvCross = Context.Create();

IntermediateRepresentation ir = spirvCross.ParseSpirV(data);

Compiler glsl = spirvCross.CreateCompiler(Backend.GLSL, ir, CaptureMode.TakeOwnership);

string source = glsl.Compile();

File.WriteAllText("files/output.comp", source);

Console.WriteLine("done without errors.");