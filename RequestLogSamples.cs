using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace RequestLogSamples
{
    public class ModuleFunctions
    {
         /* 
         * ДИСКЛЕЙМЕР: 
         * Примеры кода не содержат всех нужных и важных проверок.
         * Так же помните об ограничениях использования разных методов в клиентском, серверном и разделяемом коде.
         */

        #region Пример отправки договора во внешнюю систему

        [Public]
        public void SendContract(Sungero.Contracts.IContract contract)
        {
            // создаем полную структуру с данными
            var data = sline.IntegrationCore.Structures.Module.Data.Create();
            // получаем и передаем финальный guid тип сущности (особенно важно для перекрытий коробочных сущностей)
            data.ObjectType = contract.GetType().GetFinalType().GetTypeGuid().ToString(); // для корректной работы используйте using Sungero.Domain.Shared; 
            // передаем ИД сущности, по ИД и гуиду типа сущности из справочника запросов можно открыть конкретную сущность.
            data.ObjectId = contract.Id;
            // заполняем имя записи, рекомендуется использование ресурсов
            data.Name = "Наименование записи исходящего запроса";
            // заполняем код подключения внешней системы, выбор варианта хранения на ваш вкус: константы, строки, или иные варианты
            data.SystemCode = "SAP";
            // (опционально) заполняем код метода внешней системы, выбор варианта хранения на ваш вкус: константы, строки, или иные варианты
            data.MethodCode = "SAP_Contract";
            // при необходимости добавления собственных заголовков, например, аутентификации
            List<sline.IntegrationCore.Structures.Module.IHeaders> headers = new List<sline.IntegrationCore.Structures.Module.IHeaders>();
            data.Headers = headers;
            // устанавливаем признак немедленной отправки, если нужно отправить сейчас, а не ждать выполнения фонового процесса
            data.InstantResponse = false;
            // преобразовываем сущность в JSON
            data.Body = ConvertContractToSap(contract);
            // создаем запись справочника исходящих запросов
            _ = sline.IntegrationCore.PublicFunctions.Module.GetOrCreateOutRequest(data);
        }
        public string ConvertContractToSap(Sungero.Contracts.IContract contract)
        {
            // создаем объект структуры. Саму структуру необходимо описать, в зависимости от закрываемой задачи
            var contractStructure = sline.Integration.Structures.Module.SendContract.Create();
            // заполняем структуру данными договора, приведу пример с идентификатором
            contractStructure.Id = contract.Id;
            // преобразовываем структуру в JSON строку, используйте изолированные области
            string body = JsonConvert.SerializeObject(contractStructure);
            return body;
        }

        #endregion

        #region Пример фиксации данных по входящему запросу

        /*
        * ОГРАНИЧЕНИЕ: 
        * Учитывая особенности сервиса интеграции, тело запроса должно быть валидным.
        * Если тело запроса невалидно, сервис интеграции не пропустит его до прикладной часты
        */

        // далее приведен пример из интеграции ОШС этого решения с комментариями

        /// <summary>
        /// Синхронизация должности
        /// </summary>
        /// <param name="jobTitleModel">Структура должности</param>
        /// <returns>Структура результата синхронизации</returns>
        [Public(WebApiRequestType = RequestType.Post)]
        public virtual Structures.Module.ISyncResult SyncJobTitle(Structures.Module.IJobTitleModel jobTitleModel)
        {
            // сериализуем структуру должности в строку
            var incomingString = IsolatedFunctions.IsoArea.SerializeStructure(jobTitleModel);
            // фиксируем текущее дату-время
            var now = Calendar.Now;
            // получаем параметр отладки
            bool isDebug = GetDebugModeSetting();
            // получаем признак захвата входящих запросов
            bool captureRequest = GetIncomingRequestSetting();

            // логируем
            if (isDebug)
            {
                Logger.WithLogger("IntegrationCore").Debug("SyncJobTitle > Start");
                Logger.WithLogger("IntegrationCore").Debug($"SyncJobTitle > Request: '{incomingString}'");
            }

            // обработка структуры и создание/обновление сущностей
            var findModel = jobTitleModel.Entity;
            var syncResult = CreateResult(findModel);
            var jobTitle = CreateOrUpdateJobTitle(jobTitleModel, syncResult, isDebug);
            if (syncResult.Result != Constants.Module.SyncResult.Error)
            {
                syncResult.Hyperlink = Hyperlinks.Get(jobTitle);
                syncResult.Entity.Id = jobTitle?.Id;
            }

            // сериализация ответа для внешней системы
            var answerString = IsolatedFunctions.IsoArea.SerializeStructure(syncResult);
            // логируем
            if (isDebug)
            {
                Logger.WithLogger("IntegrationCore").Debug($"SyncJobTitle > Answer: '{answerString}'");
                Logger.WithLogger("IntegrationCore").Debug("SyncJobTitle > Completed");
            }
            // создаем входящий запрос
            if (captureRequest)
            {
                // делаем дату "красивой"
                string date = now.ToString("dd.MM.yyyy HH:mm:ss");

                // непосредственно создаем записи справочника входящие запросы
                CreateIncomingRequest(sline.IntegrationCore.Resources.IncRequest_NameFormat(Sungero.Company.Resources.Jobtitle, date),
                                      jobTitle?.GetType().GetFinalType().GetTypeGuid().ToString(), findModel, incomingString, answerString);
            }

            return syncResult;
        }

        #endregion

    }
}