using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace MSBuildProjectTools.LanguageServer.SemanticModel
{
    /// <summary>
    ///     An unused property (i.e. a <see cref="ProjectPropertyElement"/> with no corresponding <see cref="ProjectProperty"/>) in an MSBuild project, usually because the condition evaluates to <c>false</c>.
    /// </summary>
    public sealed class MSBuildUnusedProperty
        : MSBuildObject<ProjectPropertyElement>
    {
        /// <summary>
        ///     Create a new <see cref="MSBuildProperty"/>.
        /// </summary>
        /// <param name="propertyElement">
        ///     An <see cref="ProjectPropertyElement"/> representing the MSBuild property.
        /// </param>
        /// <param name="declaringElement">
        ///     An <see cref="XSElement"/> representing the property's declaring XML element.
        /// </param>
        public MSBuildUnusedProperty(ProjectPropertyElement propertyElement, XSElement declaringElement)
            : base(propertyElement, declaringElement)
        {
        }

        /// <summary>
        ///     The property name.
        /// </summary>
        public override string Name => PropertyElement.Name;

        /// <summary>
        ///     The <see cref="XSElement"/> that declares the property.
        /// </summary>
        public XSElement Element => (XSElement)base.Xml;

        /// <summary>
        ///     The kind of MSBuild object represented by the <see cref="MSBuildUnusedProperty"/>.
        /// </summary>
        public override MSBuildObjectKind Kind => MSBuildObjectKind.UnusedProperty;

        /// <summary>
        ///     The full path of the file where the target is declared.
        /// </summary>
        public override string SourceFile => PropertyElement.Location.File;

        /// <summary>
        ///     The property's raw (unevaluated) value.
        /// </summary>
        public string Value => PropertyElement.Value;

        /// <summary>
        ///     The underlying MSBuild <see cref="ProjectPropertyElement"/>.
        /// </summary>
        public ProjectPropertyElement PropertyElement => UnderlyingObject;
    }
}
