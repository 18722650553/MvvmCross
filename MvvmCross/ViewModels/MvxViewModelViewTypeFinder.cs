// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MS-PL license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using MvvmCross.IoC;
using MvvmCross.Logging;
using MvvmCross.Views;

namespace MvvmCross.ViewModels
{
#nullable enable
    public class MvxViewModelViewTypeFinder
        : IMvxViewModelTypeFinder
    {
        private readonly IMvxViewModelByNameLookup _viewModelByNameLookup;
        private readonly IMvxNameMapping _viewToViewModelNameMapping;

        public MvxViewModelViewTypeFinder(IMvxViewModelByNameLookup viewModelByNameLookup, IMvxNameMapping viewToViewModelNameMapping)
        {
            _viewModelByNameLookup = viewModelByNameLookup;
            _viewToViewModelNameMapping = viewToViewModelNameMapping;
        }

        public virtual Type? FindTypeOrNull(Type candidateType)
        {
            if (!CheckCandidateTypeIsAView(candidateType))
                return null;

            if (!candidateType.IsConventional())
                return null;

            var typeByAttribute = LookupAttributedViewModelType(candidateType);
            if (typeByAttribute != null)
                return typeByAttribute;

            var concrete = LookupAssociatedConcreteViewModelType(candidateType);
            if (concrete != null)
                return concrete;

            var typeByName = LookupNamedViewModelType(candidateType);
            if (typeByName != null)
                return typeByName;

            MvxLogHost.Default?.Log(LogLevel.Warning, "No view model association found for candidate view {name}", candidateType.Name);
            return null;
        }

        protected virtual Type? LookupAttributedViewModelType(Type candidateType)
        {
            var attribute = candidateType
                .GetCustomAttributes(typeof(MvxViewForAttribute), false)
                .FirstOrDefault() as MvxViewForAttribute;

            return attribute?.ViewModel;
        }

        protected virtual Type LookupNamedViewModelType(Type candidateType)
        {
            var viewName = candidateType.Name;
            var viewModelName = _viewToViewModelNameMapping.Map(viewName);

            _viewModelByNameLookup.TryLookupByName(viewModelName, out Type toReturn);
            return toReturn;
        }

        protected virtual Type? LookupAssociatedConcreteViewModelType(Type candidateType)
        {
            var viewModelPropertyInfo =
                Array.Find(candidateType.GetProperties(),
                    x => x.Name == "ViewModel" &&
                         !x.PropertyType.GetTypeInfo().IsInterface &&
                         !x.PropertyType.GetTypeInfo().IsAbstract);

            return viewModelPropertyInfo?.PropertyType;
        }

        protected virtual bool CheckCandidateTypeIsAView(Type candidateType)
        {
            if (candidateType == null)
                return false;

            if (candidateType.GetTypeInfo().IsAbstract)
                return false;

            if (!typeof(IMvxView).IsAssignableFrom(candidateType))
                return false;

            return true;
        }
    }
#nullable restore
}
