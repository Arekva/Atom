namespace Atom.Engine.Shader;

[Module(ShaderStageFlags.Task_NV, "*.task.spv")] 
public interface ITaskModule : IRasterModule { }