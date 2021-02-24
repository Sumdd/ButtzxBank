using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SqlSugar;
using System.Data;
using System.Collections;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace ButtzxBank
{
    /// <summary>
    /// 数据库操作类
    /// <![CDATA[
    /// 注意事项：如果语句执行时间可能会很久，需进行如下操作
    /// 1.对应数据库超时时间需设置为0无限刷
    /// 2.对应链接对象超时时间设置为0无限刷
    /// ]]>
    /// </summary>
    public class m_cSQL
    {
        #region ***示例
        public static void demo()
        {
            ///不自动关闭连接
            using (SqlSugarClient m_pEsyClient = new m_cSugar(null, false).EasyClient)
            {
                ///设置超时时间为0
                m_pEsyClient.Ado.CommandTimeOut = 0;

                ///SQL语句
                string m_sSQL = $@"
SELECT 1;
";
                ///执行语句
                m_pEsyClient.Ado.ExecuteCommand(m_sSQL);

                ///略
            }
        }
        #endregion

        #region ***6.0导案逻辑
        public static bool m_fImportCase60(DataSet m_pDataSet, bool test = false)
        {
            #region ***默认分案机构Id
            string m_sSQLGetBaseOrganiseId = $@"
SELECT TOP 1
    Id
FROM BaseOrganise WITH (NOLOCK)
WHERE ISNULL(IsDel, 0) = 0
      AND FatherId NOT IN ( SELECT Id FROM BaseOrganise WITH (NOLOCK) )
ORDER BY (
             CASE WHEN NAME LIKE '%总部%' THEN 1
                  ELSE 100
             END
         ),
         AddTime ASC;
";
            string m_sBaseOrganiseId = m_cSQL.m_fGetScalar(m_sSQLGetBaseOrganiseId, connectionString.CenoSystem60)?.ToString();

            #endregion

            #region ***默认客户Id
            string m_sSQLGetKehuId = $@"
SELECT TOP 1
    Id
FROM Xykehu WITH (NOLOCK)
WHERE ISNULL(IsDel, 0) = 0
      AND Name LIKE '%中信%'
ORDER BY AddTime ASC;
";
            string m_sKehuId = m_cSQL.m_fGetScalar(m_sSQLGetKehuId, connectionString.CenoSystem60)?.ToString();
            #endregion

            #region ***默认案件类型
            string m_sSQLGetAjlxId = $@"
SELECT TOP 1
    Id
FROM Ajlx WITH (NOLOCK)
WHERE ISNULL(IsDel, 0) = 0
      AND Name LIKE '%信用卡%'
ORDER BY AddTime ASC;
";
            string m_sAjlxId = m_cSQL.m_fGetScalar(m_sSQLGetAjlxId, connectionString.CenoSystem60)?.ToString();
            #endregion

            using (SqlSugarClient client = new m_cSugar(connectionString.CenoSystem60, false).EasyClient)
            {
                client.Open();
                client.Ado.CommandTimeOut = 0;

                #region ***创建临时表

                #region ***委外案件池信息临时表
                {
                    string m_sCaseTmpSQL = $@"
CREATE TABLE [#Case]
(
	case_flag CHAR(1),
	case_caseId CHAR(20),
	case_rrn BIGINT,
    case_agentId CHAR(20),
    case_branchId CHAR(20),
    case_adjustAreaCode VARCHAR(20),
    case_afterAreaCode VARCHAR(20),
    case_batchNum VARCHAR(20),
    case_custName NVARCHAR(20),
    case_cidDES VARCHAR(20),
    case_age VARCHAR(4),
    case_acctIdDES VARCHAR(20),
    case_acctIdENC VARCHAR(256),
    case_currency CHAR(3),
    case_gender CHAR(1),
    case_principalOpsAmt VARCHAR(20),
    case_balanceOpsAmt VARCHAR(20),
    case_lastBalanceOpsAmt VARCHAR(20),
    case_monthBalanceAmt VARCHAR(20),
    case_overPeriod NVARCHAR(50),
    case_targetPeriod NVARCHAR(50),
    case_caseType CHAR(2),
    case_entrustStartDate CHAR(20),
    case_entrustEndDate CHAR(20),
    case_isSued CHAR(1)
);";
                    client.Ado.ExecuteCommand(m_sCaseTmpSQL);
                    SqlConnection conn = (SqlConnection)(client.Ado.Connection);
                    SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.CheckConstraints, null);
                    bulkCopy.BulkCopyTimeout = int.MaxValue;
                    bulkCopy.BatchSize = 800;//经过实验获得的最快的数值
                    bulkCopy.DestinationTableName = "#Case";
                    bulkCopy.WriteToServer(m_pDataSet.Tables["委外案件池信息"]);
                    bulkCopy.Close();
                }
                #endregion

                #region ***委外案件基本信息临时表,因为信息大多无用暂时略

                #endregion

                #region ***委外案件账户信息
                {
                    string m_sAcctTmpSQL = $@"
CREATE TABLE [#Acct]
(
	acct_acctIdENC VARCHAR(128),
	acct_acctIdDES VARCHAR(40),
	acct_acctPdt NVARCHAR(5),
	acct_currency CHAR(6),
	acct_caseId CHAR(20),
	acct_rdCorCustNbr VARCHAR(26),
	acct_rdCustNbr VARCHAR(40),
	acct_lastPayMonth CHAR(20),
	acct_entrustStartDate CHAR(10),
	acct_entrustEndDate CHAR(10),
	acct_cardId VARCHAR(19),
	acct_balanceOpsAmt VARCHAR(20),
	acct_principalOpsAmt VARCHAR(20),
	acct_accAmt VARCHAR(20),
	acct_overPeriod NVARCHAR(50),
	acct_outsourceTimes VARCHAR(14)
);";
                    client.Ado.ExecuteCommand(m_sAcctTmpSQL);
                    SqlConnection conn = (SqlConnection)(client.Ado.Connection);
                    SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.CheckConstraints, null);
                    bulkCopy.BulkCopyTimeout = int.MaxValue;
                    bulkCopy.BatchSize = 800;//经过实验获得的最快的数值
                    bulkCopy.DestinationTableName = "#Acct";
                    bulkCopy.WriteToServer(m_pDataSet.Tables["委外案件账户信息"]);
                    bulkCopy.Close();
                }
                #endregion

                #region ***委外案件客户信息临时表
                {
                    string m_sCustTmpSQL = $@"
CREATE TABLE [#Cust]
(
	cust_caseId CHAR(20),
	cust_currUserId CHAR(20),
	cust_custName NVARCHAR(30),
	cust_custename VARCHAR(40),
	cust_cidType CHAR(1),
	cust_cidDES VARCHAR(18),
	cust_gender CHAR(1),
	cust_nation CHAR(3),
	cust_custmprov NVARCHAR(30),
	cust_custmcity NVARCHAR(30),
	cust_married CHAR(1),
	cust_companyName NVARCHAR(50),
	cust_position NVARCHAR(50),
	cust_workId VARCHAR(24),
	cust_mail VARCHAR(50),
	cust_cellphoneDES VARCHAR(30),
	cust_cellphoneRSA VARCHAR(344),
	cust_cellphoneRSAD VARCHAR(30),
	cust_custmaddrDES NVARCHAR(120),
	cust_custmzip VARCHAR(20),
	cust_custphoneDES VARCHAR(30),
    cust_custphoneRSA VARCHAR(344),
    cust_custphoneRSAD VARCHAR(30),
	cust_custaddrDES NVARCHAR(120),
	cust_custcity NVARCHAR(30),
	cust_custprov NVARCHAR(30),
	cust_custzip VARCHAR(20),
	cust_custemptelDES VARCHAR(30),
	cust_custemptelRSA VARCHAR(344),
	cust_custemptelRSAD VARCHAR(30),
	cust_custempaDES NVARCHAR(120),
	cust_custempaz VARCHAR(10),
	cust_custempctc NVARCHAR(40),
	cust_custglnam NVARCHAR(60),
	cust_custglrln NVARCHAR(25),
	cust_custgsex CHAR(1),
	cust_custgemp NVARCHAR(55),
	cust_custgwrkidDES VARCHAR(30),
	cust_custgwrkidRSA VARCHAR(344),
	cust_custgwrkidRSAD VARCHAR(30),
	cust_custgphoneDES VARCHAR(30),
	cust_custgphoneRSA VARCHAR(344),
	cust_custgphoneRSAD VARCHAR(30),
	cust_custgemptlDES VARCHAR(30),
	cust_custgemptlRSA VARCHAR(344),
	cust_custgemptlRSAD VARCHAR(30),
	cust_custgoccDES VARCHAR(30),
	cust_custgoccRSA VARCHAR(344),
	cust_custgoccRSAD VARCHAR(30),
	cust_custgempaDES NVARCHAR(120),
	cust_custgcity NVARCHAR(40),
	cust_custgprov NVARCHAR(40),
	cust_custgempaz VARCHAR(10),
	cust_custrfname NVARCHAR(60),
	cust_custrfrln NVARCHAR(40),
	cust_custrfmblpDES VARCHAR(30),
	cust_custrfmblpRSA VARCHAR(344),
	cust_custrfmblpRSAD VARCHAR(30),
	cust_custrfphnoDES VARCHAR(30),
	cust_custrfphnoRSA VARCHAR(344),
	cust_custrfphnoRSAD VARCHAR(30),
	cust_custrfofpnDES VARCHAR(30),
	cust_custrfofpnRSA VARCHAR(344),
	cust_custrfofpnRSAD VARCHAR(30),
	cust_custcname NVARCHAR(55),
	cust_policeregAddrDES NVARCHAR(300),
	cust_serveAddrDES NVARCHAR(300),
	cust_policestaAddrDES NVARCHAR(300)
);";
                    client.Ado.ExecuteCommand(m_sCustTmpSQL);
                    SqlConnection conn = (SqlConnection)(client.Ado.Connection);
                    SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.CheckConstraints, null);
                    bulkCopy.BulkCopyTimeout = int.MaxValue;
                    bulkCopy.BatchSize = 800;//经过实验获得的最快的数值
                    bulkCopy.DestinationTableName = "#Cust";
                    bulkCopy.WriteToServer(m_pDataSet.Tables["委外案件客户信息"]);
                    bulkCopy.Close();
                }
                #endregion

                #region ***委外案件客户邮寄积分信息临时表
                {
                    string m_sAddiTmpSQL = $@"
CREATE TABLE [#Addi]
(
	addi_caseId CHAR(20),
	addi_addressId CHAR(21),
	addi_mailName NVARCHAR(400),
	addi_mailAddressDES NVARCHAR(400),
	addi_mailPhoneDES VARCHAR(50),
	addi_mailPhoneRSA VARCHAR(344),
	addi_mailPhoneRSAD VARCHAR(50),
	addi_mailHomePhoneDES VARCHAR(50),
	addi_mailHomePhoneRSA VARCHAR(344),
	addi_mailHomePhoneRSAD VARCHAR(50),
	addi_csgName NVARCHAR(100),
	addi_csgAddressDES NVARCHAR(400),
	addi_csgPhone1DES VARCHAR(50),
	addi_csgPhone1RSA VARCHAR(344),
	addi_csgPhone1RSAD VARCHAR(50),
	addi_csgPhone2DES VARCHAR(50),
	addi_csgPhone2RSA VARCHAR(344),
	addi_csgPhone2RSAD VARCHAR(50)
);";
                    client.Ado.ExecuteCommand(m_sAddiTmpSQL);
                    SqlConnection conn = (SqlConnection)(client.Ado.Connection);
                    SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.CheckConstraints, null);
                    bulkCopy.BulkCopyTimeout = int.MaxValue;
                    bulkCopy.BatchSize = 800;//经过实验获得的最快的数值
                    bulkCopy.DestinationTableName = "#Addi";
                    bulkCopy.WriteToServer(m_pDataSet.Tables["委外案件客户邮寄积分信息"]);
                    bulkCopy.Close();
                }
                #endregion

                #region ***委外案件客户联系方式临时表
                {
                    string m_sCntaTmpSQL = $@"
CREATE TABLE [#Cnta]
(
	cnta_caseId CHAR(20),
	cnta_phoneId CHAR(21),
	cnta_phoneType CHAR(1),
	cnta_relation CHAR(1),
	cnta_name NVARCHAR(10),
	cnta_phoneDES VARCHAR(30),
	cnta_phoneRSA VARCHAR(344),
	cnta_phoneRSAD VARCHAR(30),
	cnta_status CHAR(1),
	cnta_origin NVARCHAR(50)
);";
                    client.Ado.ExecuteCommand(m_sCntaTmpSQL);
                    SqlConnection conn = (SqlConnection)(client.Ado.Connection);
                    SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.CheckConstraints, null);
                    bulkCopy.BulkCopyTimeout = int.MaxValue;
                    bulkCopy.BatchSize = 800;//经过实验获得的最快的数值
                    bulkCopy.DestinationTableName = "#Cnta";
                    bulkCopy.WriteToServer(m_pDataSet.Tables["委外案件客户联系方式"]);
                    bulkCopy.Close();
                }
                #endregion

                #region ***委外案件客户联系方式临时表
                {
                    string m_sAddrTmpSQL = $@"
CREATE TABLE [#Addr]
(
	addr_caseId CHAR(20),
	addr_addressId CHAR(21),
	addr_name NVARCHAR(50),
	addr_relation CHAR(1),
	addr_addressType CHAR(1),
	addr_addressDES NVARCHAR(1000),
	addr_origin NVARCHAR(50)
);";
                    client.Ado.ExecuteCommand(m_sAddrTmpSQL);
                    SqlConnection conn = (SqlConnection)(client.Ado.Connection);
                    SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.CheckConstraints, null);
                    bulkCopy.BulkCopyTimeout = int.MaxValue;
                    bulkCopy.BatchSize = 800;//经过实验获得的最快的数值
                    bulkCopy.DestinationTableName = "#Addr";
                    bulkCopy.WriteToServer(m_pDataSet.Tables["委外案件客户联系地址"]);
                    bulkCopy.Close();
                }
                #endregion

                #endregion

                #region ***案件信息导入
                string m_sSQL = $@"
                                            DECLARE @InPiciId						NVARCHAR(100)=dbo.getNewId(NEWID()),
                                                    @AddTime                        datetime=getdate(),
                                                    @AddUserId                      NVARCHAR(70)='zxAuto',
                                                    @m_sKehuId                      VARCHAR(70)='{m_sKehuId}',
                                                    @m_sAjlxId                      VARCHAR(70)='{m_sAjlxId}',
                                                    @m_sBaseOrganiseId              VARCHAR(70)='{m_sBaseOrganiseId}'
-- 先补充批次表
                                          INSERT INTO dbo.InPici
                                                    ( Id ,
                                                      Code ,
                                                      XykehuId ,
                                                      Jdsj ,
                                                      AddUserId ,
                                                      AddTime 
                                                    )
                                            select top 1 @InPiciId ,
                                                      ISNULL(t.Prefix,'')+'_'+dbo.dateFormat(@AddTime,'yyMMdd')+dbo.dateFormat(GETDATE(),'hhnnss'),
                                                      t.Id ,
                                                      @AddTime ,
                                                      @AddUserId ,
                                                      GETDATE() 
                                              from XyKehu t WITH(NOLOCK)
                                              WHERE t.Id = @m_sKehuId;

-- 卡信息
SELECT dbo.getNewId(NEWID()) as Id,
       @InPiciId AS InPiciId,
       ('中信' + case_caseId) AS Code,
       (cust_cidDES + '_' + case_caseId) AS Shfzh,
       case_custName AS Xm,
       case_gender AS Sex,
       -- case_age AS age,
       @m_sKehuId AS XyKehuId,
       @m_sAjlxId AS AjlxId,
       NULL AS PiciId,
       acct_acctIdDES AS Account,
       acct_cardId AS Cardno,
       NULL AS khrq,
       case_currency AS HuoBi,
       ISNULL(case_balanceOpsAmt,'0') AS Qkje,
       case_balanceOpsAmt AS Zhrmb,
       acct_balanceOpsAmt AS Zxqke,
       NULL AS Rate,
       NULL AS Yhlx,
       NULL AS Zdr,
       NULL AS Klms,
       NULL AS Xsqd,
       NULL AS Tgno,
       case_overPeriod AS Gqsd,
       NULL AS Zfk,
       NULL AS Remark1,
       NULL AS Remark2,
       NULL AS Remark3,
       NULL AS LastShuaKaRq,
       NULL AS LastTiXianRq,
       NULL AS KehuAjBh,
       NULL AS Edu,
       NULL AS Rcrq,
       case_principalOpsAmt AS Qkbj,
       NULL AS Qkznj,
       NULL AS NianFei,
       NULL AS ChaoxianFei,
       NULL AS QitaFei,
       NULL AS Fyze,
       cust_custmprov AS Shengfen,
       cust_custmcity AS Chengshi,
       cust_companyName AS CompanyName,
       NULL AS CompanyAddress,
       @AddUserId AS AddUserId,
       @AddTime AS AddTime,
       NULL AS XyqitaId,
       NULL AS LastCardInfoAssignId,
       NULL AS LastXyStateChangeId,
       cust_position AS Zhiwu,
       -- case_entrustStartDate AS Jdsj,
       case_entrustEndDate AS Dqsj,
       NULL AS Zjjkrq,
       NULL AS zjjkj,
       acct_lastPayMonth AS Zxqkerq,
       NULL AS GqsdVal,
       NULL AS jqje,
       NULL AS wyj,
       NULL AS fx,
       NULL AS ljYqsd,
       NULL AS dkye,
       case_branchId AS KehuFh,
       NULL AS Branch,
       NULL AS Remark4,
       NULL AS Remark5,
       NULL AS Remark6,
       NULL AS Remark7,
       NULL AS Remark8,
       NULL AS KeHuBianHao,
       NULL AS HeTongBianHao,
       case_afterAreaCode AS DoAreaTrue,
       case_adjustAreaCode AS DoArea,
       NULL AS CIF,
       case_batchNum AS WeiWaiPiCiHao,
       NULL AS NeiBuShouBie,
       NULL AS NeiBuYuQi,
       dbo.getNewId(NEWID()) AS XybaseId,
       NULL AS Proportion,
       @m_sBaseOrganiseId AS RelationId,
       'BaseOrganise' AS BaseRelationId,
       @AddTime AS ChangeTime,
       'xyState_New' AS State,
       @AddTime AS StateChangeTime,
       case_acctIdENC AS accountENC,
       case_acctIdDES AS accountDES,
       case_rrn AS rrn,
       case_caseId AS caseId
       -- ,
       -- cust_cidDES AS shfzhDES,
       -- cust_cidType AS shfzhType,
       -- case_isSued AS isSuSong
INTO #tCardInfo
FROM #Acct WITH (NOLOCK)
    LEFT JOIN #Case WITH (NOLOCK)
        ON #Case.case_caseId = #Acct.acct_caseId
    LEFT JOIN #Cust WITH (NOLOCK)
        ON #Cust.cust_caseId = #Acct.acct_caseId;

-- 提取基本信息
                                SELECT tCard.XybaseId AS Id,
		                                tCard.InPiciId,
		                                  tCard.Shfzh,
		                                  tCard.AjlxId,
		                                  tCard.XyKehuId,
		                                  'b'+MIN(Code) AS Code,
                                          sum(convert(float, tCard.Qkje)) as DzdZxqke,
                                          tCard.KeHuBianHao
                                INTO #Xybase
                                  FROM #tCardInfo tCard WITH(NOLOCK)
                                 GROUP BY tCard.XybaseId,
                                          tCard.InPiciId,
		                                  tCard.Shfzh,
		                                  tCard.AjlxId,
		                                  tCard.XyKehuId,
                                          tCard.KeHuBianHao
-- 提取卡信息
                            INSERT INTO dbo.CardInfo
                                        ( Id,
                                          InPiciId ,
                                          Code ,
                                          Shfzh ,
                                          Xm ,
                                          Sex ,
                                          XyKehuId ,
                                          AjlxId ,
                                          PiciId,
                                          Account ,
                                          Cardno ,
                                          khrq ,
                                          Huobi ,
                                          Qkje ,
                                          Zhrmb ,
                                          Zxqke ,
                                          Rate ,
                                          Yhlx ,
                                          Zdr ,
                                          Klms ,
                                          Xsqd ,
                                          Tgno ,
                                          Gqsd ,
                                          Zfk ,
                                          Remark1 ,
                                          Remark2 ,
                                          Remark3 ,
                                          LastShuaKaRq ,
                                          LastTiXianRq ,
                                          KehuAjBh ,
                                          Edu ,
                                          Rcrq ,
                                          Qkbj ,
                                          Qkznj ,
                                          NianFei ,
                                          ChaoxianFei ,
                                          QitaFei ,
                                          Fyze ,
                                          Shengfen ,
                                          Chengshi ,
                                          CompanyName ,
                                          CompanyAddress ,
                                          AddUserId ,
                                          AddTime,
                                          XyqitaId,
                                          LastCardInfoAssignId,
                                          LastXyStateChangeId,
                                          Zhiwu,
                                          Dqsj,
                                          Zjjkrq,
                                          zjjkj,
                                          Zxqkerq,
                                          GqsdVal,
                                          jqje,
                                          wyj,
                                          fx,
                                          ljYqsd,
                                          dkye,
                                          KehuFh,
                                          Branch,
                                          Remark4,
                                          Remark5,
                                          Remark6,
                                          Remark7,
                                          Remark8,
                                          KeHuBianHao,
                                          HeTongBianHao,
                                          DoAreaTrue,
                                          DoArea,
                                          CIF,
                                          WeiWaiPiCiHao,
                                          NeiBuShouBie,
                                          NeiBuYuQi,
                                          XybaseId,
                                          Proportion,
                                         -- WeiAnQiXian
                                          RelationId ,
                                          BaseRelationId ,
                                          ChangeTime,
                                          State ,
                                          StateChangeTime,
                                          accountENC,
                                          accountDES,
                                          rrn,
                                          caseId
                                        )
                                select tCard.*
                                  from #tCardInfo tCard
                                 
                                INSERT INTO dbo.Xybase
                                ( 
                                  Id,
		                          InPiciId,
		                          Shfzh,
		                          AjlxId,
		                          XyKehuId,
		                          Code ,
                                  DzdZxqke,
                                  KeHuBianHao,
                                  AddUserId ,
                                  AddTime
                                )
                                select *,
                                        @AddUserId as AddUserId,
                                        @AddTime as AddTime
                                from #Xybase tBase 

-- 导入地址,客户信息横过来插入
                                         SELECT tCard.Id as CardInfoId,
                                                tCard.Shfzh AS Shfzh,
												-- 实际关系
												case
                                                WHEN AddressClass IN ('cust_companyName','cust_custmaddrDES','cust_custaddrDES','cust_custempaDES','cust_custcname','cust_policeregAddrDES','cust_serveAddrDES','cust_policestaAddrDES') THEN '本人'
												WHEN AddressClass IN ('cust_custgemp','cust_custgempaDES') THEN cust_custglrln
                                                END AS Relation,--和本人关系
												-- 实际姓名
												case
												WHEN AddressClass IN ('cust_companyName','cust_custmaddrDES','cust_custaddrDES','cust_custempaDES','cust_custcname','cust_policeregAddrDES','cust_serveAddrDES','cust_policestaAddrDES') THEN cust_custName
												WHEN AddressClass IN ('cust_custgemp','cust_custgempaDES') THEN cust_custglnam
                                                END AS Xm,--姓名
												case
                                                WHEN AddressClass IN ('cust_companyName','cust_custcname','cust_custgemp') THEN '单位名称'
												WHEN AddressClass IN ('cust_custmaddrDES') THEN '通讯地址'
												WHEN AddressClass IN ('cust_custaddrDES') THEN '住宅地址'
												WHEN AddressClass IN ('cust_custempaDES','cust_custgempaDES') THEN '单位地址'
												WHEN AddressClass IN ('cust_policeregAddrDES') THEN '公安户籍地址'
												WHEN AddressClass IN ('cust_serveAddrDES') THEN '服务住所地址'
												WHEN AddressClass IN ('cust_policestaAddrDES') THEN '所管辖派出所'
                                                END AS Lx,--地址类型
												case
												WHEN AddressClass IN ('cust_custmaddrDES') THEN cust_custmzip
												WHEN AddressClass IN ('cust_custaddrDES') THEN cust_custzip
												WHEN AddressClass IN ('cust_custempaDES') THEN cust_custempaz
												WHEN AddressClass IN ('cust_custgempaDES') THEN cust_custgempaz
                                                END AS Zip,--邮编
												-- 脱敏类型需要记录
												case
												WHEN AddressClass IN ('cust_custmaddrDES') THEN '0213'
												WHEN AddressClass IN ('cust_custaddrDES') THEN '0211'
												WHEN AddressClass IN ('cust_custempaDES') THEN '0212'
												WHEN AddressClass IN ('cust_custgempaDES') THEN '0232'
												WHEN AddressClass IN ('cust_policeregAddrDES') THEN '0011'
												WHEN AddressClass IN ('cust_serveAddrDES') THEN '0012'
												WHEN AddressClass IN ('cust_policestaAddrDES') THEN '0013'
                                                END AS decodeType,--地址类型
                                                t_UNPIVOT_p.tAddress as addressDES,
												@AddTime AS AddTime,
                                                @AddUserId AS AddUserId,
                                                tCard.Xm AS ZwrXm
										INTO #tempAddress
                                         FROM 
										 (
											select 
												cust_caseId,
												cust_custglrln,
												cust_custrfrln,
												cust_custName,
												cust_custglnam,
												cust_custmzip,
												cust_custzip,
												cust_custempaz,
												cust_custgempaz,
												CONVERT(nvarchar(300),cust_companyName) AS cust_companyName,
												CONVERT(nvarchar(300),cust_custmaddrDES) AS cust_custmaddrDES,
												CONVERT(nvarchar(300),cust_custaddrDES) AS cust_custaddrDES,
												CONVERT(nvarchar(300),cust_custempaDES) AS cust_custempaDES,
												CONVERT(nvarchar(300),cust_custgemp) AS cust_custgemp,
												CONVERT(nvarchar(300),cust_custgempaDES) AS cust_custgempaDES,
												CONVERT(nvarchar(300),cust_custcname) AS cust_custcname,
												CONVERT(nvarchar(300),cust_policeregAddrDES) AS cust_policeregAddrDES,
												CONVERT(nvarchar(300),cust_serveAddrDES) AS cust_serveAddrDES,
												CONVERT(nvarchar(300),cust_policestaAddrDES) AS cust_policestaAddrDES
											from #Cust with(nolock)
										 ) p
                                                UNPIVOT
                                            (tAddress FOR AddressClass IN ( cust_companyName,cust_custmaddrDES,cust_custaddrDES,cust_custempaDES,cust_custgemp,
											cust_custgempaDES,cust_custcname,cust_policeregAddrDES,cust_serveAddrDES,cust_policestaAddrDES)) t_UNPIVOT_p
                                        left join #tCardInfo tCard with(nolock)
                                        on(t_UNPIVOT_p.cust_caseId=tCard.caseId)
                                        WHERE ISNULL(tAddress,'')<>''

                                        INSERT into XyAddress
                                         (
     		                                    CardInfoId,--案件编号 
                                                Shfzh,--身份证号
                                                relation,--和本人关系
                                                xm,--姓名
                                                lx,--类型
                                                SrcAddress,
                                                Address,
                                                Zip,
                                                SourceId,--KehuId
                                                Source,
                                                AddUserId,
                                                AddTime,
												addressDES,
												decodeType,
                                                ZwrXm
                                         )
                                        select t.CardInfoId,--案件编号 
                                               t.Shfzh,--身份证号
                                               t.relation,
                                               t.Xm,--姓名
                                               t.lx,--类型
                                                t.addressDES,
                                                t.addressDES,
                                                t.Zip,
                                                @m_sKehuId AS XyKehuId,--KehuId,
                                                tKehu.Name,
                                               t.AddUserId,
                                               t.AddTime,
											   t.addressDES,
											   t.decodeType,
                                               t.ZwrXm
                                          from #tempAddress t
                                        left join Xykehu tKehu with(nolock)
                                        on(@m_sKehuId=tKehu.Id)
                                           where not exists(SELECT top 1 1
                                                          FROM XyAddress told with(nolock)
                                                         where isnull(told.isdel,0)=0
                                                           --修正共案问题,只要该案件没有该地址就要添加,或者这里直接去掉不要,将所有地址无论是否重复直接添加
                                                           and isnull(told.CardInfoId,'')=isnull(t.CardInfoId,'')
                                                           and isnull(told.addressDES,'')=isnull(t.addressDES,'')
                                                           and told.SourceId=isnull(@m_sKehuId,'')
                                                           and told.xm=isnull(t.Xm,'')
                                                           and told.Shfzh=isnull(t.Shfzh,''));
                                        drop table #tempAddress;
-- 导入地址

										INSERT INTO XyAddress
                                         (
     		                                    CardInfoId,--案件ID 
                                                Shfzh,--身份证号
                                                relation,--和本人关系
                                                xm,--姓名
                                                lx,--联系方式
                                                SrcAddress,
                                                Address,
                                                SourceId,--KehuId
                                                Source,
                                                AddUserId,
                                                AddTime,
                                                addressDES,
                                                ZwrXm,
                                                addressId
                                         )
                                        select tCard.Id AS CardInfoId,--案件编号 
                                               tCard.Shfzh AS Shfzh,--身份证号
                                               --tRelation.Id,
                                               case when addr_relation = '1' then '本人'
                                                    when addr_relation = '2' then '亲属'
                                                    when addr_relation = '3' then '非亲属'
                                               end as relation,
                                               addr_name AS xm,--姓名
                                               case when addr_addressType = '1' then '家庭电话'
                                                    when addr_addressType = '2' then '手机'
                                                    when addr_addressType = '3' then '单位电话'
                                               end as lx,
                                               addr_addressDES as SrcAddress,
                                               addr_addressDES as [Address],
                                               case when ISNULL(addr_origin,'1') = '1' then tKehu.Id
												     else NULL
												end as SourceId,
												case when ISNULL(addr_origin,'1') = '1' then tKehu.Name
												     else addr_origin
												end as [Source],
                                               @AddUserId AS AddUserId,
                                               @AddTime AS AddTime,
											   addr_addressDES,
                                               tCard.Xm AS ZwrXm,
                                               addr_addressId AS phoneId
                                          from #Addr t 
                                        left join #tCardInfo tCard with(nolock)
                                        on(t.addr_caseId=tCard.caseId)
                                        left join Xykehu tKehu with(nolock)
                                        on(@m_sKehuId=tKehu.Id)
                                        where ISNULL(t.addr_addressDES,'')<>''
                                          and not exists(SELECT top 1 1
                                                          FROM XyAddress told with(nolock)
                                                         where isnull(told.isdel,0)=0
                                                           --修正共案问题,只要该案件没有该地址就要添加,或者这里直接去掉不要,将所有地址无论是否重复直接添加
                                                           and isnull(told.CardInfoId,'')=isnull(tCard.Id,'')
                                                           and isnull(told.addressDES,'')=isnull(t.addr_addressDES,'')
                                                           and told.SourceId=isnull(@m_sKehuId,'')
                                                           and told.xm=isnull(t.addr_name,'')
                                                           and told.Shfzh=isnull(tCard.Shfzh,''));

-- 导入电话,列转行

										SELECT tCard.Id as CardInfoId,
                                               tCard.Shfzh AS Shfzh,
											   -- 默认简写
                                                /*case
                                                WHEN PhoneClass IN ('cust_cellphoneDES','cust_custphoneDES','cust_custemptelDES') THEN '本人'
												WHEN PhoneClass IN ('cust_custgwrkidDES','cust_custgphoneDES','cust_custgemptlDES','cust_custgoccDES') THEN '非亲属'
												WHEN PhoneClass IN ('cust_custrfmblpDES','cust_custrfphnoDES','cust_custrfofpnDES') THEN '亲属'
                                                END AS Relation,--和本人关系
												*/
												-- 实际关系
												case
                                                WHEN PhoneClass IN ('cust_cellphoneDES','cust_custphoneDES','cust_custemptelDES') THEN '本人'
												WHEN PhoneClass IN ('cust_custgwrkidDES','cust_custgphoneDES','cust_custgemptlDES','cust_custgoccDES') THEN cust_custglrln
												WHEN PhoneClass IN ('cust_custrfmblpDES','cust_custrfphnoDES','cust_custrfofpnDES') THEN
													case when cust_custrfrln = '1' then '配偶'
														 when cust_custrfrln = '2' then '子女'
													     when cust_custrfrln = '3' then '父母'
													     when cust_custrfrln = '4' then '兄弟姐妹'
														 when cust_custrfrln = 'G' then '兄弟姐妹'
														 else '其它'
													end
                                                END AS Relation,--和本人关系
												-- 实际关系
												case
                                                WHEN PhoneClass IN ('cust_cellphoneDES','cust_custphoneDES','cust_custemptelDES') THEN cust_custName
												WHEN PhoneClass IN ('cust_custgwrkidDES','cust_custgphoneDES','cust_custgemptlDES','cust_custgoccDES') THEN cust_custglnam
												WHEN PhoneClass IN ('cust_custrfmblpDES','cust_custrfphnoDES','cust_custrfofpnDES') THEN cust_custrfname
                                                END AS Xm,--姓名
												case
                                                WHEN PhoneClass IN ('cust_cellphoneDES','cust_custgwrkidDES','cust_custrfmblpDES') THEN '手机'
												WHEN PhoneClass IN ('cust_custphoneDES','cust_custgphoneDES','cust_custrfphnoDES') THEN '家庭电话'
												WHEN PhoneClass IN ('cust_custemptelDES') THEN '单位电话'
												WHEN PhoneClass IN ('cust_custgemptlDES','cust_custrfofpnDES') THEN '办电'
												WHEN PhoneClass IN ('cust_custgoccDES') THEN '办电分机'
                                                END AS Lx,--号码类型
                                                t_UNPIVOT_p.tPhone as PhoneDES,
                                                @AddTime AS AddTime,
                                                @AddUserId AS AddUserId,
												case
                                                WHEN PhoneClass IN ('cust_cellphoneDES') THEN cust_cellphoneRSA
												WHEN PhoneClass IN ('cust_custphoneDES') THEN cust_custphoneRSA
												WHEN PhoneClass IN ('cust_custemptelDES') THEN cust_custemptelRSA
												WHEN PhoneClass IN ('cust_custgwrkidDES') THEN cust_custgwrkidRSA
												WHEN PhoneClass IN ('cust_custgphoneDES') THEN cust_custgphoneRSA
												WHEN PhoneClass IN ('cust_custgemptlDES') THEN cust_custgemptlRSA
												WHEN PhoneClass IN ('cust_custgoccDES') THEN cust_custgoccRSA
												WHEN PhoneClass IN ('cust_custrfmblpDES') THEN cust_custrfmblpRSA
												WHEN PhoneClass IN ('cust_custrfphnoDES') THEN cust_custrfphnoRSA
												WHEN PhoneClass IN ('cust_custrfofpnDES') THEN cust_custrfofpnRSA
                                                END AS PhoneRSA,--加密串
												case
                                                WHEN PhoneClass IN ('cust_cellphoneDES') THEN cust_cellphoneRSAD
												WHEN PhoneClass IN ('cust_custphoneDES') THEN cust_custphoneRSAD
												WHEN PhoneClass IN ('cust_custemptelDES') THEN cust_custemptelRSAD
												WHEN PhoneClass IN ('cust_custgwrkidDES') THEN cust_custgwrkidRSAD
												WHEN PhoneClass IN ('cust_custgphoneDES') THEN cust_custgphoneRSAD
												WHEN PhoneClass IN ('cust_custgemptlDES') THEN cust_custgemptlRSAD
												WHEN PhoneClass IN ('cust_custgoccDES') THEN cust_custgoccRSAD
												WHEN PhoneClass IN ('cust_custrfmblpDES') THEN cust_custrfmblpRSAD
												WHEN PhoneClass IN ('cust_custrfphnoDES') THEN cust_custrfphnoRSAD
												WHEN PhoneClass IN ('cust_custrfofpnDES') THEN cust_custrfofpnRSAD
                                                END AS PhoneRSAD,--解密串
                                                tCard.Xm AS ZwrXm
										INTO #tempPhone
                                         FROM #Cust p
                                                 UNPIVOT
                                               (tPhone FOR PhoneClass IN (cust_cellphoneDES,cust_custphoneDES,cust_custemptelDES,cust_custgwrkidDES,cust_custgphoneDES,cust_custgemptlDES,cust_custgoccDES,
											   cust_custrfmblpDES,cust_custrfphnoDES,cust_custrfofpnDES)) t_UNPIVOT_p
                                        left join #tCardInfo tCard with(nolock)
                                        on(t_UNPIVOT_p.cust_caseId=tCard.caseId)
                                        WHERE ISNULL(tPhone,'')<>''

                                         INSERT INTO XyPhone
                                         (
     		                                    CardInfoId,--案件ID 
                                                Shfzh,--身份证号
                                                relation,--和本人关系
                                                xm,--姓名
                                                lx,--联系方式
                                                SrcPhone,
                                                phone,
                                                SourceId,--KehuId
                                                Source,
                                                AddUserId,
                                                AddTime,
                                                PhoneDES,
                                                PhoneRSA,
                                                ZwrXm
                                         )
                                        select t.CardInfoId,--案件编号 
                                               t.Shfzh,--身份证号
                                               --tRelation.Id,
                                               t.relation,
                                               t.xm,--姓名
                                               t.lx,--类型
                                               t.PhoneRSAD,
                                               t.PhoneRSAD,
                                               tKehu.Id AS XyKehuId,--KehuId,
                                               tKehu.Name,
                                               t.AddUserId,
                                               t.AddTime,
											   t.PhoneDES,
											   t.PhoneRSA,
											   t.ZwrXm
                                          from #tempPhone t  
                                        left join Xykehu tKehu with(nolock)
                                        on(@m_sKehuId=tKehu.Id)
                                        /*left join BaseSysCode tRelation with(nolock)
                                        on(t.relation=tRelation.Code
                                        and tRelation.BaseSysCodeGroupId='ContactRelationship'
                                        and isnull(tRelation.IsDel,0)=0)*/
                                         where not exists(SELECT top 1 1
                                                          FROM xyphone told with(nolock)
                                                         where isnull(told.isdel,0)=0
                                                           --修正共案问题,只要该案件没有电话就要添加,或者这里直接去掉不要,将所有电话无论是否重复直接添加
                                                           and isnull(told.CardInfoId,'')=isnull(t.CardInfoId,'')
                                                           and told.phone=isnull(t.PhoneRSAD,'')
                                                           and told.Shfzh=isnull(t.Shfzh,'')
                                                           and told.SourceId=isnull(@m_sKehuId,'')
                                                           and told.xm=isnull(t.xm,''));
                                        drop table #tempPhone;
-- 导入电话

										 INSERT INTO XyPhone
                                         (
     		                                    CardInfoId,--案件ID 
                                                Shfzh,--身份证号
                                                relation,--和本人关系
                                                xm,--姓名
                                                lx,--联系方式
                                                SrcPhone,
                                                phone,
                                                SourceId,--KehuId
                                                Source,
                                                AddUserId,
                                                AddTime,
                                                PhoneDES,
                                                PhoneRSA,
                                                YouXiaoXing,
                                                ZwrXm,
                                                phoneId
                                         )
                                        select tCard.Id AS CardInfoId,--案件编号 
                                               tCard.Shfzh AS Shfzh,--身份证号
                                               --tRelation.Id,
                                               case when cnta_relation = '1' then '本人'
                                                    when cnta_relation = '2' then '亲属'
                                                    when cnta_relation = '3' then '非亲属'
                                               end as relation,
                                               cnta_name AS xm,--姓名
                                               case when cnta_phoneType = '1' then '家庭电话'
                                                    when cnta_phoneType = '2' then '手机'
                                                    when cnta_phoneType = '3' then '单位电话'
                                               end as lx,
                                               cnta_phoneRSAD as SrcPhone,
                                               cnta_phoneRSAD as phone,
                                               case when ISNULL(cnta_origin,'1') = '1' then tKehu.Id
												     else NULL
												end as SourceId,
												case when ISNULL(cnta_origin,'1') = '1' then tKehu.Name
												     else cnta_origin
												end as [Source],
                                               @AddUserId AS AddUserId,
                                               @AddTime AS AddTime,
											   cnta_phoneDES,
											   cnta_phoneRSA,
											   case when cnta_status = '0' then '有效'
													when cnta_status = '1' then '无效'
													else null
												end as YouXiaoXing,
                                               tCard.Xm AS ZwrXm,
                                               cnta_phoneId AS phoneId
                                          from #Cnta t 
                                        left join #tCardInfo tCard with(nolock)
                                        on(t.cnta_caseId=tCard.caseId)
                                        left join Xykehu tKehu with(nolock)
                                        on(@m_sKehuId=tKehu.Id)
                                        where ISNULL(t.cnta_phoneDES,'')<>''
                                          and not exists(SELECT top 1 1
                                                          FROM xyphone told with(nolock)
                                                         where isnull(told.isdel,0)=0
                                                           --修正共案问题,只要该案件没有电话就要添加,或者这里直接去掉不要,将所有电话无论是否重复直接添加
                                                           and isnull(told.CardInfoId,'')=isnull(tCard.Id,'')
                                                           and told.phone=isnull(t.cnta_phoneRSAD,'')
                                                           and told.Shfzh=isnull(tCard.Shfzh,'')
                                                           and told.SourceId=isnull(@m_sKehuId,'')
                                                           and told.xm=isnull(t.cnta_name,''));

-- 导入备注,略

-- 测试回滚
-- select 1 / 0;

-- 删除缓存
DROP TABLE #tCardInfo;
DROP TABLE #Xybase;
DROP TABLE #Case;
DROP TABLE #Acct;
DROP TABLE #Cust;
DROP TABLE #Addi;
DROP TABLE #Cnta;
DROP TABLE #Addr;
";
                #endregion

                try
                {
                    client.Ado.BeginTran();
                    client.Ado.ExecuteCommand(m_sSQL);
                    client.Ado.CommitTran();
                }
                catch (Exception ex)
                {
                    Log.Instance.Debug(ex, LogTyper.ProLogger);
                    client.Ado.RollbackTran();
                    return false;
                }
                return true;
            }
        }
        #endregion

        #region ***创建临时表
        public static string m_fCreateTempTable(DataTable dtUserData, bool m_bString, string m_bTableName = "tUserData")
        {
            if (!string.IsNullOrWhiteSpace(dtUserData.TableName)) m_bTableName = dtUserData.TableName;

            string createTempTablesql = @"CREATE TABLE #{1}
                                            (
                                                {0}
                                            );";
            Dictionary<Type, string> dataDictionary = new Dictionary<Type, string>();
            dataDictionary[typeof(decimal)] = dataDictionary[typeof(double)] = " decimal(18,6) ";
            dataDictionary[typeof(int)] = dataDictionary[typeof(long)] = " bigint ";
            dataDictionary[typeof(string)] = " nvarchar(max) ";
            dataDictionary[typeof(DateTime)] = " DateTime ";
            dataDictionary[typeof(object)] = " nvarchar(max) ";

            List<string> colSqlList = new List<string>();
            foreach (DataColumn col in dtUserData.Columns)
            {
                string colDataType = dataDictionary[col.DataType];
                if (col.DataType != typeof(DateTime) && m_bString) colDataType = " nvarchar(max) ";

                string colSql = string.Format(@" [{0}] {1} ", col.ColumnName, colDataType);
                colSqlList.Add(colSql);
            }
            createTempTablesql = string.Format(createTempTablesql,
                string.Join(",", colSqlList), m_bTableName);
            return createTempTablesql;
        }
        #endregion

        #region ***返回数据表
        public static DataTable m_fGetDataTable(string m_sSQL, string m_sConnStr = null)
        {
            SqlSugarClient m_sEsyClient = new m_cSugar(m_sConnStr).EasyClient;
            return m_sEsyClient.Ado.GetDataTable(m_sSQL);
        }
        #endregion

        #region ***返回第一行第一列
        public static object m_fGetScalar(string m_sSQL, string m_sConnStr = null)
        {
            SqlSugarClient m_sEsyClient = new m_cSugar(m_sConnStr).EasyClient;
            return m_sEsyClient.Ado.GetScalar(m_sSQL);
        }
        #endregion
    }
}