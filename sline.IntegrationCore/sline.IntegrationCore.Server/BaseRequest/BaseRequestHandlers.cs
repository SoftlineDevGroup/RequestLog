using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using sline.IntegrationCore.BaseRequest;

namespace sline.IntegrationCore
{
  partial class BaseRequestServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Created = Calendar.Now;
    }
  }

  partial class BaseRequestFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null || query == null)
        return query;
      
      #region Фильтрация по дате создания запроса
      
      var beginDate = Calendar.SqlMinValue;
      var endDate = Calendar.SqlMaxValue;
      
      if (_filter.Last7days)
        beginDate = Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-7)).BeginningOfDay();
      
      if (_filter.Last14days)
        beginDate = Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-14)).BeginningOfDay();
      
      if (_filter.Last30days)
        beginDate = Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-30)).BeginningOfDay();
      
      if (_filter.Last365days)
        beginDate = Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-365)).BeginningOfDay();
      
      if (_filter.ManualPeriod)
      {
        beginDate = _filter.DateRangeFrom ?? Calendar.SqlMinValue;
        endDate = _filter.DateRangeTo ?? Calendar.SqlMaxValue;
      }
      
      var serverPeriodBegin = Equals(Calendar.SqlMinValue, beginDate) ? beginDate : Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(beginDate);
      var serverPeriodEnd = Equals(Calendar.SqlMaxValue, endDate) ? endDate : endDate.EndOfDay().FromUserTime();
      
      // Если временные оффсеты клиента и сервера не отличаются, то отфильтровать документы по периоду.
      if (TenantInfo.UtcOffset == Users.UtcOffsetOfCurrent)
      {
        query = query.Where(x => x.Created.Between(serverPeriodBegin, serverPeriodEnd));
      }
      else
      {
        // Если временные оффсеты клиента и сервера отличаются.
        var clientPeriodEnd = !Equals(Calendar.SqlMaxValue, endDate) ? endDate.AddDays(1) : Calendar.SqlMaxValue;
        query = query.Where(x => (x.Created.Between(serverPeriodBegin, serverPeriodEnd) ||
                                  x.Created == beginDate) && x.Created != clientPeriodEnd);
      }
      
      #endregion
      
      return query;
    }
  }

}