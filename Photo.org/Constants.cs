using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Photo.org
{
    internal enum Component { Photos, Viewer };
    internal enum SortBy { Filename, Folder, Filesize, ImportDate, Width, Height, Resolution, EXIFDate, Random };

    internal static class Guids
    {
        internal static readonly Guid AllFiles = new Guid("9f773ba9-f97c-443c-ab10-d749c4d544ca");
        internal static readonly Guid Unassigned = new Guid("72cba7a2-2e28-410e-a5b9-119c3779ba8e");
        internal static readonly Guid Hidden = new Guid("316a70d0-2882-4867-acd6-0808c6b36ec2");
    }

    internal static class Setting
    {
        //internal static readonly string ImportPath = "importPath";
        internal static readonly string DatabaseFilename = "databaseFilename";
    }

    internal static class PhotoCategorySource
    {
        internal static readonly string AddedByUser = "U";
    }

    internal static class KeyStates
    {
        internal static readonly int Shift = 4;
    }
}
