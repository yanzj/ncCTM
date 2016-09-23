﻿using System;
using System.Collections.Generic;
using System.Linq;
using CTM.Core.Domain.Industry;

namespace CTM.Services.Industry
{
    public partial interface IIndustryService : IBaseService
    {
        IList<IndustryInfo> GetAllIndustry(bool showDeleted = false);

        string GetIndustryNameById(int industryId);
    }
}