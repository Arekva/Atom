namespace Atom.Engine.Shader;

[Module(ShaderStageFlags.TessellationEvaluation, "*.tese.spv")] 
public interface ITessellationEvaluationModule : IRasterModule   { }