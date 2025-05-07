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
         * ����������: 
         * ������� ���� �� �������� ���� ������ � ������ ��������.
         * ��� �� ������� �� ������������ ������������� ������ ������� � ����������, ��������� � ����������� ����.
         */

        #region ������ �������� �������� �� ������� �������

        [Public]
        public void SendContract(Sungero.Contracts.IContract contract)
        {
            // ������� ������ ��������� � �������
            var data = sline.IntegrationCore.Structures.Module.Data.Create();
            // �������� � �������� ��������� guid ��� �������� (�������� ����� ��� ���������� ���������� ���������)
            data.ObjectType = contract.GetType().GetFinalType().GetTypeGuid().ToString(); // ��� ���������� ������ ����������� using Sungero.Domain.Shared; 
            // �������� �� ��������, �� �� � ����� ���� �������� �� ����������� �������� ����� ������� ���������� ��������.
            data.ObjectId = contract.Id;
            // ��������� ��� ������, ������������� ������������� ��������
            data.Name = "������������ ������ ���������� �������";
            // ��������� ��� ����������� ������� �������, ����� �������� �������� �� ��� ����: ���������, ������, ��� ���� ��������
            data.SystemCode = "SAP";
            // (�����������) ��������� ��� ������ ������� �������, ����� �������� �������� �� ��� ����: ���������, ������, ��� ���� ��������
            data.MethodCode = "SAP_Contract";
            // ��� ������������� ���������� ����������� ����������, ��������, ��������������
            List<sline.IntegrationCore.Structures.Module.IHeaders> headers = new List<sline.IntegrationCore.Structures.Module.IHeaders>();
            data.Headers = headers;
            // ������������� ������� ����������� ��������, ���� ����� ��������� ������, � �� ����� ���������� �������� ��������
            data.InstantResponse = false;
            // ��������������� �������� � JSON
            data.Body = ConvertContractToSap(contract);
            // ������� ������ ����������� ��������� ��������
            _ = sline.IntegrationCore.PublicFunctions.Module.GetOrCreateOutRequest(data);
        }
        public string ConvertContractToSap(Sungero.Contracts.IContract contract)
        {
            // ������� ������ ���������. ���� ��������� ���������� �������, � ����������� �� ����������� ������
            var contractStructure = sline.Integration.Structures.Module.SendContract.Create();
            // ��������� ��������� ������� ��������, ������� ������ � ���������������
            contractStructure.Id = contract.Id;
            // ��������������� ��������� � JSON ������, ����������� ������������� �������
            string body = JsonConvert.SerializeObject(contractStructure);
            return body;
        }

        #endregion

        #region ������ �������� ������ �� ��������� �������

        /*
        * �����������: 
        * �������� ����������� ������� ����������, ���� ������� ������ ���� ��������.
        * ���� ���� ������� ���������, ������ ���������� �� ��������� ��� �� ���������� �����
        */

        // ����� �������� ������ �� ���������� ��� ����� ������� � �������������

        /// <summary>
        /// ������������� ���������
        /// </summary>
        /// <param name="jobTitleModel">��������� ���������</param>
        /// <returns>��������� ���������� �������������</returns>
        [Public(WebApiRequestType = RequestType.Post)]
        public virtual Structures.Module.ISyncResult SyncJobTitle(Structures.Module.IJobTitleModel jobTitleModel)
        {
            // ����������� ��������� ��������� � ������
            var incomingString = IsolatedFunctions.IsoArea.SerializeStructure(jobTitleModel);
            // ��������� ������� ����-�����
            var now = Calendar.Now;
            // �������� �������� �������
            bool isDebug = GetDebugModeSetting();
            // �������� ������� ������� �������� ��������
            bool captureRequest = GetIncomingRequestSetting();

            // ��������
            if (isDebug)
            {
                Logger.WithLogger("IntegrationCore").Debug("SyncJobTitle > Start");
                Logger.WithLogger("IntegrationCore").Debug($"SyncJobTitle > Request: '{incomingString}'");
            }

            // ��������� ��������� � ��������/���������� ���������
            var findModel = jobTitleModel.Entity;
            var syncResult = CreateResult(findModel);
            var jobTitle = CreateOrUpdateJobTitle(jobTitleModel, syncResult, isDebug);
            if (syncResult.Result != Constants.Module.SyncResult.Error)
            {
                syncResult.Hyperlink = Hyperlinks.Get(jobTitle);
                syncResult.Entity.Id = jobTitle?.Id;
            }

            // ������������ ������ ��� ������� �������
            var answerString = IsolatedFunctions.IsoArea.SerializeStructure(syncResult);
            // ��������
            if (isDebug)
            {
                Logger.WithLogger("IntegrationCore").Debug($"SyncJobTitle > Answer: '{answerString}'");
                Logger.WithLogger("IntegrationCore").Debug("SyncJobTitle > Completed");
            }
            // ������� �������� ������
            if (captureRequest)
            {
                // ������ ���� "��������"
                string date = now.ToString("dd.MM.yyyy HH:mm:ss");

                // ��������������� ������� ������ ����������� �������� �������
                CreateIncomingRequest(sline.IntegrationCore.Resources.IncRequest_NameFormat(Sungero.Company.Resources.Jobtitle, date),
                                      jobTitle?.GetType().GetFinalType().GetTypeGuid().ToString(), findModel, incomingString, answerString);
            }

            return syncResult;
        }

        #endregion

    }
}