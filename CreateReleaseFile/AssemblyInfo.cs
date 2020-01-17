using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

namespace GiroZilla
{
    public class AssemblyInfo
    {
        private static string root = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).ToString()).ToString()).ToString();
        private static string assemblyPath = Path.Combine(root, @"GiroZilla\obj\Release\GiroZilla.exe");

        // The assembly information values.
        public string Title = "";

        public string Description = "";

        public string Company = "";

        public string Product = "";

        public string Copyright = "";

        public string Trademark = "";

        public string AssemblyVersion;

        public string FileVersion = "";

        public string Guid = "";

        public string NeutralLanguage = "";

        public bool IsComVisible;

        private static T GetAssemblyAttribute<T>(Assembly assembly) where T : Attribute
        {
            // Get attributes of this type.
            var attributes = assembly.GetCustomAttributes(typeof(T), true);

            // If we didn't get anything, return null.
            if (attributes.Length == 0)
            {
                return null;
            }

            // Convert the first attribute value into
            // the desired type and return it.
            return (T)attributes[0];
        }

        public AssemblyInfo() : this(Assembly.LoadFile(assemblyPath))
        {

        }

        public AssemblyInfo(Assembly assembly)
        {
            // Get values from the assembly.

            // Title
            var titleAttr = GetAssemblyAttribute<AssemblyTitleAttribute>(assembly);
            if (titleAttr != null)
            {
                Title = titleAttr.Title;
            }

            // Description
            var assemblyAttr = GetAssemblyAttribute<AssemblyDescriptionAttribute>(assembly);
            if (assemblyAttr != null) 
            {
                Description = assemblyAttr.Description;
            }

            // Company
            var companyAttr = GetAssemblyAttribute<AssemblyCompanyAttribute>(assembly);
            if (companyAttr != null)
            {
                Company = companyAttr.Company;
            }

            // Product
            var productAttr = GetAssemblyAttribute<AssemblyProductAttribute>(assembly);
            if (productAttr != null)
            {
                Product = productAttr.Product;
            }

            // Copyright
            var copyrightAttr = GetAssemblyAttribute<AssemblyCopyrightAttribute>(assembly);
            if (copyrightAttr != null)
            {
                Copyright = copyrightAttr.Copyright;
            }

            // Trademark
            var trademarkAttr = GetAssemblyAttribute<AssemblyTrademarkAttribute>(assembly);
            if (trademarkAttr != null)
            {
                Trademark = trademarkAttr.Trademark;
            }

            AssemblyVersion = assembly.GetName().Version.ToString();

            // File Version
            var fileVersionAttr = GetAssemblyAttribute<AssemblyFileVersionAttribute>(assembly);
            if (fileVersionAttr != null)
            {
                FileVersion = fileVersionAttr.Version;
            }

            // Guid
            var guidAttr = GetAssemblyAttribute<GuidAttribute>(assembly);
            if (guidAttr != null)
            {
                Guid = guidAttr.Value;
            }

            // Neutral Language
            var languageAttr = GetAssemblyAttribute<NeutralResourcesLanguageAttribute>(assembly);
            if (languageAttr != null)
            {
                NeutralLanguage = languageAttr.CultureName;
            }

            // IsComVisible
            var comAttr = GetAssemblyAttribute<ComVisibleAttribute>(assembly);
            if (comAttr != null)
            {
                IsComVisible = comAttr.Value;
            }
        }
    }
}
