using Atom.Engine.Vulkan;

namespace Atom.Engine.RenderPass;

public class RenderPassBuilder
{
    private HashSet<Attachment> _attachments;
    private Dictionary<uint, Subpass> _subpasses;
    private HashSet<Dependency> _dependencies;

    public RenderPassBuilder()
    {
        _attachments = new HashSet<Attachment>();
        _subpasses = new Dictionary<uint, Subpass>();
        _dependencies = new HashSet<Dependency>();
    }

    public RenderPassBuilder(IEnumerable<Subpass> subpasses, IEnumerable<Dependency> dependencies) : this()
    {
        uint pass = 0U;
        foreach (Subpass subpass in subpasses)
        {
            LinkSubpass(subpass, pass++);
        }

        foreach (Dependency dependency in dependencies)
        {
            LinkDependency(dependency);
        }
    }

    public unsafe vk.Result Build(vk.Device device, out Silk.NET.Vulkan.RenderPass renderPass,
        vk.RenderPassCreateFlags flags = 0)
    {
        Attachment[] all_attachments =
            _attachments
                .Concat(_subpasses.Values
                    .SelectMany(pass => pass.Attachments))
                .Distinct().ToArray();
        Dictionary<Attachment, uint> attachments = new(all_attachments.Length);
        vk.AttachmentDescription* vk_attachments = stackalloc vk.AttachmentDescription[all_attachments.Length];

        uint i = 0U;
        foreach (Attachment attachment in all_attachments)
        {
            vk_attachments[i] = attachment.Description;
            attachments.Add(attachment, i++);
        }

        vk.SubpassDescription* vk_subpasses = stackalloc vk.SubpassDescription[_subpasses.Count];
        Dictionary<Subpass, uint> subpasses = new(_subpasses.Count);

        vk.AttachmentReference* depth_attachments = stackalloc vk.AttachmentReference[_subpasses.Count];
        i = 0U;
        foreach (Subpass subpass in _subpasses.Values)
        {
#pragma warning disable CA2014
            uint j = 0U;
            int input_count = subpass.InputAttachments.Count;
            vk.AttachmentReference* inputs = stackalloc vk.AttachmentReference[input_count];
            foreach ((Attachment attach, vk.ImageLayout layout) in subpass.InputAttachments)
            {
                inputs[j++] = new vk.AttachmentReference(attachments[attach], layout);
            }

            j = 0U;
            int color_count = subpass.ColorAttachments.Count;
            vk.AttachmentReference* colors = stackalloc vk.AttachmentReference[color_count];
            foreach ((Attachment attach, vk.ImageLayout layout) in subpass.ColorAttachments)
            {
                colors[j++] = new vk.AttachmentReference(attachments[attach], layout);
            }
            
            j = 0U;
            int resolves_count = subpass.ResolveAttachments.Count;
            vk.AttachmentReference* resolves = stackalloc vk.AttachmentReference[resolves_count];
            foreach ((Attachment attach, vk.ImageLayout layout) in subpass.ResolveAttachments)
            {
                resolves[j++] = new vk.AttachmentReference(attachments[attach], layout);
            }

            j = 0U;
            int preserve_count = subpass.PreserveAttachments.Count;
            uint* preserve = stackalloc uint[preserve_count];
            foreach (Attachment attach in subpass.PreserveAttachments)
            {
                preserve[j++] = attachments[attach];
            }

            vk_subpasses[(int)i] = new vk.SubpassDescription(
                flags: subpass.Flags,
                pipelineBindPoint: subpass.BindPoint,
                inputAttachmentCount: (uint)input_count,
                pInputAttachments: inputs,
                colorAttachmentCount: (uint)subpass.ColorAttachments.Count,
                pColorAttachments: colors,
                pResolveAttachments: resolves,
                //pDepthStencilAttachment: ,
                preserveAttachmentCount: (uint)subpass.PreserveAttachments.Count,
                pPreserveAttachments: preserve
            );
            
            // Set depth (if exists)
            if (subpass.DepthStencilAttachment != null)
            {
                depth_attachments[i] = new vk.AttachmentReference(
                    attachments[subpass.DepthStencilAttachment.Value.Item1],
                    layout: subpass.DepthStencilAttachment.Value.Item2
                );

                vk_subpasses[(int)i].PDepthStencilAttachment = depth_attachments + i;
            }

            subpasses.Add(subpass, i++);
#pragma warning restore CA2014
        }

        int dependency_count = _dependencies.Count;
        i = 0;
        vk.SubpassDependency* vk_dependencies = stackalloc vk.SubpassDependency[dependency_count];
        foreach (Dependency dependency in _dependencies)
        {
            vk_dependencies[i++] = new vk.SubpassDependency(
                srcSubpass: dependency.Source.Subpass == Subpass.External 
                    ? vk.Vk.SubpassExternal 
                    : subpasses[dependency.Source.Subpass],
                dstSubpass: dependency.Destination.Subpass == Subpass.External 
                    ? vk.Vk.SubpassExternal 
                    : subpasses[dependency.Destination.Subpass],
                srcStageMask: dependency.Source.StageMask.ToVk(),
                dstStageMask: dependency.Destination.StageMask.ToVk(),
                
                srcAccessMask: dependency.Source.AccessMask,
                dstAccessMask: dependency.Destination.AccessMask,
                
                dependencyFlags: dependency.Flags
            );
        }

        vk.RenderPassCreateInfo info = new(
            flags: flags,
            attachmentCount: (uint)all_attachments.Length,
            pAttachments: vk_attachments,
            subpassCount: (uint)subpasses.Count,
            pSubpasses: vk_subpasses,
            dependencyCount: (uint)dependency_count,
            pDependencies: vk_dependencies
        );
        
        return VK.API.CreateRenderPass(device, in info, null, out renderPass);
    }


    public void LinkDependency(Dependency dependency) => _dependencies.Add(dependency);
    public void UnlinkDependency(Dependency dependency) => _dependencies.Remove(dependency);

    public void LinkSubpass(Subpass subpass, uint order)
    {
        if (_subpasses.ContainsKey(order))
        {
            throw new Exception($"A subpass is already present at order {order}");
        }

        _subpasses.Add(order, subpass);
    }

    public void MoveSubpass(Subpass subpass, uint newOrder)
    {
        if (_subpasses.ContainsValue(subpass))
        {
            // the subpass exist
            if (_subpasses.ContainsKey(newOrder))
            {
                if (_subpasses[newOrder] != subpass)
                {
                    // another subpass set at destination order
                    throw new Exception($"Another subpass is already set at order {newOrder}");
                }

                // do nothing, destination order is same as previous one
            }
            else
            {
                // the order is not occupied, process to switching
                uint currentOrder = _subpasses.First(kvp => kvp.Value == subpass).Key;
                _subpasses.Remove(currentOrder);
                _subpasses.Add(newOrder, subpass);
            }
        }
        else
        {
            // the subpass hasn't been added
            throw new Exception("Subpass is not assigned to this render pass.");
        }
    }

    public void UnlinkSubpass(Subpass subpass)
    {
        try
        {
            _subpasses.Remove(_subpasses.First(kvp => kvp.Value == subpass).Key);
        }
        catch (Exception _)
        {
            throw new Exception("Subpass is not assigned to this render pass.");
        }
    }

    public void LinkAttachment(Attachment attachment) => _attachments.Add(attachment);
    public void UnlinkAttachment(Attachment attachment) => _attachments.Remove(attachment);
}