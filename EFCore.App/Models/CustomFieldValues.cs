using System;
using System.Collections.Generic;

namespace EFCore.App.Models
{
    public partial class CustomFieldValues
    {
        public int Id { get; set; }
        public int? CustomFieldId { get; set; }
        public int? ProjectId { get; set; }
    }
}
