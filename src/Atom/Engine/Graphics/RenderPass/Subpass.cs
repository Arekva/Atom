using Silk.NET.Vulkan;

namespace Atom.Engine.RenderPass;

public class Subpass
{
    public SubpassDescriptionFlags Flags { get; set; }
        public PipelineBindPoint BindPoint { get; set; }

        internal Dictionary<Attachment, ImageLayout> InputAttachments = new();
        internal Dictionary<Attachment, ImageLayout> ColorAttachments = new();
        internal Dictionary<Attachment, ImageLayout> ResolveAttachments = new();
        internal (Attachment, ImageLayout)? DepthStencilAttachment = null;
        internal List<Attachment> PreserveAttachments = new();

        internal uint SpecialId { get; set; } = 0; // used for VK_SUBPASS_EXTERNAL

        public static Subpass External { get; } = new(PipelineBindPoint.Graphics)
            { SpecialId = Silk.NET.Vulkan.Vk.SubpassExternal };

        public List<Attachment> Attachments
        {
            get
            {
                List<Attachment> attachments = PreserveAttachments
                    .Concat(InputAttachments.Keys
                        .Concat(ColorAttachments.Keys
                            .Concat(ResolveAttachments.Keys)))
                    .Distinct()
                    .ToList();
                if (DepthStencilAttachment != null)
                {
                    attachments.Add(DepthStencilAttachment.Value.Item1);
                }

                return attachments;
            }
        }

        public Subpass(PipelineBindPoint bindPoint = PipelineBindPoint.Graphics) => BindPoint = bindPoint;

        public Subpass(
            IEnumerable<Attachment>? inputs = null,
            IEnumerable<Attachment>? colors = null,
            IEnumerable<Attachment>? resolves = null,
            IEnumerable<Attachment>? depthStencils = null,
            IEnumerable<Attachment>? preserves = null,
            PipelineBindPoint bindPoint = PipelineBindPoint.Graphics) : this(bindPoint)
        {
            if (inputs != null)
            {
                foreach (Attachment attach in inputs)
                {
                    AssignInput(attach, ImageLayout.ShaderReadOnlyOptimal);
                }
            }

            if (colors != null)
            {
                foreach (Attachment attach in colors)
                {
                    AssignColor(attach, ImageLayout.ColorAttachmentOptimal);
                }
            }

            if (resolves != null)
            {
                foreach (Attachment attach in resolves)
                {
                    AssignResolve(attach, ImageLayout.ColorAttachmentOptimal);
                }
            }

            if (depthStencils != null)
            {
                foreach (Attachment attach in depthStencils)
                {
                    AssignDepthStencil(attach, ImageLayout.DepthStencilAttachmentOptimal);
                }
            }

            if (preserves != null)
            {
                foreach (Attachment attach in preserves)
                {
                    AssignPreserve(attach);
                }
            }
        }

        // input
        public void AssignInput(Attachment attachment, ImageLayout layout) => InputAttachments.Add(attachment, layout);

        public void UnassignInput(Attachment attachment) => InputAttachments.Remove(attachment);

        // color
        public void AssignColor(Attachment attachment, ImageLayout layout) => ColorAttachments.Add(attachment, layout);

        public void UnassignColor(Attachment attachment) => ColorAttachments.Remove(attachment);

        // resolve
        public void AssignResolve(Attachment attachment, ImageLayout layout) =>
            ResolveAttachments.Add(attachment, layout);

        public void UnassignResolve(Attachment attachment) => ResolveAttachments.Remove(attachment);

        // depth
        public void AssignDepthStencil(Attachment attachment, ImageLayout layout) =>
            DepthStencilAttachment = (attachment, layout);

        public void UnassignDepthStencil() => DepthStencilAttachment = null;

        // preserve
        public void AssignPreserve(Attachment attachment) => PreserveAttachments.Add(attachment);
        public void UnassignPreserve(Attachment attachment) => PreserveAttachments.Remove(attachment);
}