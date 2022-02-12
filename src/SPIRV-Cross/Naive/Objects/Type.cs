using SPIRVCross.Base;
using static SPIRVCross.Base.SPIRV;

namespace SPIRVCross.Naive
{
    public class Type : ISpvcObject<spvc_type>
    {
        public nint Handle { get; set; }

        public bool ArrayDimensionIsLiteral(u32 dimension) => spvc_type_array_dimension_is_literal(this, dimension);
        public u32 GetArrayDimension(u32 dimension) => spvc_type_get_array_dimension(this, dimension);
        public u32 GetBaseTypeId() => spvc_type_get_base_type_id(this);
        public BaseType GetBaseType() => (BaseType)spvc_type_get_basetype(this);
        public u32 GetBitWidth() => spvc_type_get_bit_width(this);
        public u32 GetColumns() => spvc_type_get_columns(this);
        public AccessQualifier GetImageAccessQualifier() => (AccessQualifier)spvc_type_get_image_access_qualifier(this);
        public bool GetImageArrayed() => spvc_type_get_image_arrayed(this);
        public Dimension GetImageDimension() => (Dimension)spvc_type_get_image_dimension(this);
        public bool GetImageIsDepth() => spvc_type_get_image_is_depth(this);
        public bool GetImageIsStorage() => spvc_type_get_image_is_storage(this);
        public bool GetImageMultisampled() => spvc_type_get_image_multisampled(this);
        public u32 GetImageSampledType() => spvc_type_get_image_sampled_type(this);
        public ImageFormat GetImageStorageFormat() => (ImageFormat)spvc_type_get_image_storage_format(this);
        public u32 GetMemberType(u32 index) => spvc_type_get_member_type(this, index);
        public u32 GetNumArrayDimensions() => spvc_type_get_num_array_dimensions(this);
        public u32 GetNumMemberTypes() => spvc_type_get_num_member_types(this);
        public StorageClass GetStorageClass() => (StorageClass)spvc_type_get_storage_class(this);
        public u32 GetVectorSize() => spvc_type_get_vector_size(this);

        public static implicit operator spvc_type(Type type) => type.ToSpvc();
    }
}