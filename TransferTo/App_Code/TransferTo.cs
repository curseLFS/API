using log4net;
using log4net.Config;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
// NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service" in code, svc and config file together.
public class TransferTo : ITransferTo
{
    private static readonly ILog kplog = LogManager.GetLogger(typeof(TransferTo));
    private DBConnection partnerNEW_ConDB;
    private DBConnection partnerOLD_ConDB;
    private String username = "", password = "",  url = "", pin = "";
    private String accountid = "";

    private Double chargetotal = 0.00;
    private Double totalrunningbalance = 0.00;
 
    private String receiptno = "";
    public TransferTo()
    {
        try
        {
            XmlConfigurator.Configure();
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });

            IniFile credfile = new IniFile("C:\\kpconfig\\APINewCredentials.ini");
            username = credfile.IniReadValue("TransferTo Credentials", "Username");
            password = credfile.IniReadValue("TransferTo Credentials", "Password");
            accountid = credfile.IniReadValue("TransferTo Credentials", "accountId");
            pin = credfile.IniReadValue("TransferTo Credentials", "pin");
            url = credfile.IniReadValue("TransferTo Credentials", "url");

            connectDB();
            connectDB_OLD();
            log4net.Config.XmlConfigurator.Configure();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
    private void connectDB() // NEW DATABASE
    {
        try
        {
            String path = "C:\\kpconfig\\APINewConf.ini";
            IniFile ini = new IniFile(path);
            String Serv = ini.IniReadValue("DBConfig Transaction", "server");
            String DB = ini.IniReadValue("DBConfig Transaction", "database");
            String UID = ini.IniReadValue("DBConfig Transaction", "uid");
            String Password = ini.IniReadValue("DBConfig Transaction", "password");
            String pool = ini.IniReadValue("DBConfig Transaction", "pool");
            Int32 maxcon = Convert.ToInt32(ini.IniReadValue("DBConfig Transaction", "maxcon"));
            Int32 mincon = Convert.ToInt32(ini.IniReadValue("DBConfig Transaction", "mincon"));
            Int32 tout = Convert.ToInt32(ini.IniReadValue("DBConfig Transaction", "tout"));

            partnerNEW_ConDB = new DBConnection(Serv, DB, UID, Password, pool, maxcon, mincon, tout);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.ToString());
        }
    }
    private void connectDB_OLD() // OLD DATABASE
    {
        try
        {
            String path = "C:\\kpconfig\\APIConf.ini";
            IniFile ini = new IniFile(path);
            String Serv = ini.IniReadValue("DBConfig Transaction", "server");
            String DB = ini.IniReadValue("DBConfig Transaction", "database");
            String UID = ini.IniReadValue("DBConfig Transaction", "uid");
            String Password = ini.IniReadValue("DBConfig Transaction", "password");
            String pool = ini.IniReadValue("DBConfig Transaction", "pool");
            Int32 maxcon = Convert.ToInt32(ini.IniReadValue("DBConfig Transaction", "maxcon"));
            Int32 mincon = Convert.ToInt32(ini.IniReadValue("DBConfig Transaction", "mincon"));
            Int32 tout = Convert.ToInt32(ini.IniReadValue("DBConfig Transaction", "tout"));
   
            partnerOLD_ConDB = new DBConnection(Serv, DB, UID, Password, pool, maxcon, mincon, tout);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.ToString());
        }
    }
    public String RequestResponse(String requestURL, String method, Object obj) // REQUEST AND RESPONSE FOR CREATE COLLECTION
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestURL);
        request.Method = method;
        request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(username + ":" + password));
        request.ContentType = "application/json;charset=UTF-8";
        request.PreAuthenticate = true;

        if (obj != null)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(obj.GetType());

            JavaScriptSerializer js = new JavaScriptSerializer();
            String data = js.Serialize(obj);
            StreamWriter writer = new StreamWriter(request.GetRequestStream());
            writer.Write(data);
            writer.Flush();
            writer.Close();
        }
        else
        {
            obj = "empty";
        }

        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        Stream stream = response.GetResponseStream();
        StreamReader reader = new StreamReader(stream);

        String resp = reader.ReadToEnd();

        return resp;
    }
    public TransResponse searchTransaction(String refno) // RETRIEVE TRANSACTION
    {
        TransResponse trans = new TransResponse();
        String pdobj = "", result = "" ;
        try
        {      
            String requestURL = url + "/v1/cash-pickup/transactions/code-" + refno;

            result = RequestResponse(requestURL, "GET", null);

            var jsonresult = JsonConvert.DeserializeObject<dynamic>(result);

            trans.respcode = "1";
            trans.respmsg = "success";
            trans.transdate = getserverdatePartners().ToString();
            trans.partnersdata = result;

            var partnersdata = new PartnersData
            {
                refno = refno,
                SenderFullName = jsonresult.sender.lastname + ", " + jsonresult.sender.firstname + " " + jsonresult.sender.middlename,
                ReceiverFullName = jsonresult.beneficiary.lastname + ", " + jsonresult.beneficiary.firstname + " " + jsonresult.sender.middlename,
                ReceiverContactNo = "",
                ReceiverAddress = jsonresult.beneficiary.address,
                Amount = jsonresult.destination.amount,
                Currency = jsonresult.destination.currency
            };

            var details = new Details
            {
                AccntCode = accountid,
                Amount = jsonresult.destination.amount,
                ReceiverAddress = jsonresult.beneficiary.address,
                ReceiverFirstName = jsonresult.beneficiary.firstname,
                ReceiverLastName = jsonresult.beneficiary.lastname,
                ReceiverMiddleName = jsonresult.beneficiary.middlename,
                ReceiverContactNo = "",
                ReceiverBirthDate = jsonresult.beneficiary.date_of_birth,
                ReceiverCity = jsonresult.beneficiary.city,
                ReceiverFullName = jsonresult.beneficiary.lastname + ", " + jsonresult.beneficiary.firstname + " " + jsonresult.beneficiary.middlename,
                Currency = jsonresult.destination.currency,
                RefNo = refno,
                SenderFullName = jsonresult.sender.lastname + ", " + jsonresult.sender.firstname + " " + jsonresult.sender.middlename,
                SenderAddress = jsonresult.sender.address,
                SenderBirthDate = jsonresult.sender.date_of_birth,
                SenderCity = jsonresult.sender.city,
            };
            pdobj = new JavaScriptSerializer().Serialize(partnersdata);

            #region SAVE LOGS
            using (MySqlConnection con = partnerNEW_ConDB.getConnection())
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = con.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "kpadminpartnerslog.api_insertpartnersdatalogs";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("_partnersid", accountid);
                        cmd.Parameters.AddWithValue("_refno", refno);
                        cmd.Parameters.AddWithValue("_sessionid", DBNull.Value);
                        cmd.Parameters.AddWithValue("_tokenid", "");
                        cmd.Parameters.AddWithValue("_partnersdata", pdobj);
                        cmd.Parameters.AddWithValue("_servername", System.Environment.MachineName.ToString());
                        int x = cmd.ExecuteNonQuery();
                    }
                    con.Close();
                }

                catch (Exception ex)
                {
                    kplog.Fatal(refno + " - " + ex.ToString());
                    con.Close();
                    throw new Exception(ex.Message);
                }
            }
            #endregion  
            
        }
        catch (TimeoutException ex)
        {
            trans.respcode = "0";
            trans.respmsg = "Unable to process request. Connection timeout occured. Please try again later.";
            kplog.Fatal(refno + " - " + ex.ToString());
        }
        catch (WebException ex)
        {
            #region
            kplog.Fatal(refno + " - " + ex.ToString());
            using (var stream = ex.Response.GetResponseStream())
            {
                using (var reader = new StreamReader(stream))
                {
                    var statuscode = ((HttpWebResponse)ex.Response).StatusCode;
                    var statusmsg = ((HttpWebResponse)ex.Response).StatusDescription;
                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        kplog.Fatal("refno: " + refno + " | Status Code : " + statuscode + " | Status Description : " + statusmsg + " | Server :" + ((HttpWebResponse)ex.Response).Server);
                    }
                    trans.respcode = Convert.ToString(statuscode);
                    trans.respmsg = statusmsg;
                    trans.transdate = Convert.ToString(getserverdatePartners());
                    kplog.Fatal(refno + " - " + ex.ToString());
                }
            }
            #endregion
        }
        catch (Exception ex)
        {
            trans.respcode = "0";
            trans.respmsg = "Unable to process request. The system encountered some technical problem. Sorry for the inconvenience.";
            kplog.Fatal(refno + " - " + ex.ToString());
        }
        kplog.Info(refno + " - " + trans.respcode + " - " + trans.respmsg + " - " + trans.transdate + " - " + trans.partnersdata);
        return trans;

    }
    
    public String InquireTransaction(String refno)
    {
            
        String requestURL = url + "/v1/cash-pickup/transactions/code-" + refno;

        var result = RequestResponse(requestURL, "GET", null);

        return result;        
    }
    public String commitToPartners(String refno, String kptn, String bcode)
    {
        TransResponse trans = new TransResponse();
        String result = "";
        var inquiretrans = InquireTransaction(refno);
        var jsoninquire = JsonConvert.DeserializeObject<dynamic>(inquiretrans);
        try
        {
            #region CREATE COLLECTION
            var collectionOJB = new collections
            {
                external_id = kptn,
                branch = new branch
                {
                    code = bcode,
                    name = jsoninquire.beneficiary.lastname + ", " + jsoninquire.beneficiary.firstname + " " + jsoninquire.beneficiary.middle,
                    //name = "Cebu Head Office",
                    address = jsoninquire.beneficiary.address,
                    //address = "B. Benedicto St., NRA, Cebu City",
                    postal_code = jsoninquire.beneficiary.postal_code,
                    //postal_code = "6000",
                    city = jsoninquire.beneficiary.city,
                    //city = "Cebu City",
                    country_iso_code = "PHL"
                },
                transaction_code = refno
            };
            #endregion
            var createtURL = url + "/v1/cash-pickup/transactions/code-" + refno + "/collections";


            string message = "";
            for (int persist = 0; persist < 3; persist++)
            {
                kplog.Info(refno + " - " + "Persist count TagAsCompleted and InquireTagAsCompleted: " + (persist + 1));
                  
                    try
                    {                  
                        kplog.Info(" TransferTo create collection request : "+ refno + " - " + createtURL);

                        result = RequestResponse(createtURL, "POST", collectionOJB);

                        kplog.Info(" TransferTo create collection response : " + refno + " - " + result);

                        #region COMPLETE TRANSACTION
                        var jsonresp = Newtonsoft.Json.JsonConvert.DeserializeObject<complete>(result);
                    if (jsonresp.status != "10000")
                    {
                        return "ERROR";
                    }
                    else 
                    {
                        var completeURL = url + "/v1/cash-pickup/collections/" + jsonresp.id + "/complete";

                        kplog.Info(" TransferTo complete collection request : " + refno + " - " + completeURL);

                        var resp = RequestResponse(completeURL, "POST", null);
                        kplog.Info(" TransferTo complete collection request : " + refno + " - TransferTo complete collection response: " + resp);
                        message =  "SUCCESS";
                        break;
                    }
                  }
                  catch (Exception ex)
                  {
                      kplog.Fatal(" refno: " + refno + " kptn: " + kptn + " errordetails: " + ex.ToString());
                      message = "FATAL: " + ex.ToString();
                  }
                Thread.Sleep((persist + 1) * 2000);
            }

            return message;
            #endregion           
        }
        catch (TimeoutException ex)
        {
            kplog.Fatal(refno + " - " + ex.ToString());           
            return "Unable to process request. Connection timeout occured. Please try again later.";
        }
        catch (WebException ex)
        {           
            #region
            kplog.Fatal(refno + " - " + ex.ToString());
            using (var stream = ex.Response.GetResponseStream())
            {
                using (var reader = new StreamReader(stream))
                {
                    var statuscode = ((HttpWebResponse)ex.Response).StatusCode;
                    var statusmsg = ((HttpWebResponse)ex.Response).StatusDescription;
                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        kplog.Fatal("refno: " + refno + " | Status Code : " + statuscode + " | Status Description : " + statusmsg + " | Server :" + ((HttpWebResponse)ex.Response).Server);
                    }
                    trans.respcode = Convert.ToString(statuscode); ;
                    trans.respmsg = statusmsg;
                    trans.transdate = Convert.ToString(getserverdatePartners());
                    kplog.Fatal(refno + " - " + ex.ToString());
                }
            }
            #endregion
            return trans.respmsg + " - Unable to process request. Failed in connecting to partners API. Please try again later.";
        }
        catch (Exception ex)
        {
            var exmsg = ex.Message;
            kplog.Fatal(refno + " - " + ex.Message.ToString());
            return exmsg + " - Unable to process request. The system encountered some technical problem. Sorry for the inconvenience.";
        }
    }

    public TransResponse saveTransaction(String refno, String sendername, String receivername, String receiveraddress, String receivercontact,
                                      String currency, String FXAmount, String transpin, String sec_Token, String series, Boolean isremote,
                                      String remoteOperator, String remotebcode, Int16 remotezcode, String remotereason, String bcode,
                                      Int16 zcode, String operatorID, String stationno, String ben_IDType, String ben_IDNo, String expirydate,
                                      String transtype, String POControl)
    {
        TransResponse transdetails = new TransResponse();
        try
        {
            kplog.Info("refno : " + refno + " sendername : " + sendername + " receivername : " + receivername + " receiveraddress : " + receiveraddress + " receivercontact : " + receivercontact + "currency : " + currency + " FXAmount : " + FXAmount + " transpin : " + transpin + " sec_Token : " + sec_Token + " series : " + series + " isremote : " + isremote + " remoteOperator : " + remoteOperator + " remotebcode: " + remotebcode + " remotezcode : " + remotezcode + " remotereason : " + remotereason + " bcode : " + bcode + " zcode : " + zcode + " operatorID :" + operatorID + " stationno :" + stationno + " ben_IDType :" + ben_IDType + " ben_IDNo :" + ben_IDNo + "expirydate:" + expirydate + " transtype :" + transtype + " POControl :" + POControl);

            DateTime dt = getserverdatePartners();
            // searched info
            String ben_IDExpDate = "", userID = "";
            // standard parameters
            String sessionid = transpin;
            string kptn = "";

            String datapass = "";

            String SOControl = "";

            sendername = sendername.Replace("-n-*", "ñ");
            receivername = receivername.Replace("-n-*", "ñ");
            receiveraddress = receiveraddress.Replace("-n-*", "ñ");
            remoteOperator = remoteOperator.Replace("-n-*", "ñ");
            remotereason = remotereason.Replace("-n-*", "ñ");
            operatorID = operatorID.Replace("-n-*", "ñ");

            if (POControl == String.Empty || POControl == "" || POControl == null)
            {

                kplog.Info("Payout control number cannot be null. " + POControl);
                return new TransResponse { respcode = "0", respmsg = "Payout control cannot be null." };
            }

            if (expirydate.ToString().Equals("") || expirydate.Equals(null) || expirydate.Equals("1901-01-01"))
                ben_IDExpDate = "01-Dec-2030";
            else
                ben_IDExpDate = Convert.ToDateTime(expirydate).ToString("dd-MM-yyyy");

            Double principal = Convert.ToDouble(FXAmount);

            if (isremote)
            {
                kptn = generateKPTNpartners(remotebcode, remotezcode);
                userID = remoteOperator;
            }
            else
            {
                kptn = generateKPTNpartners(bcode, zcode);
                userID = operatorID;
            }

            if (isMLpaidout(refno, accountid))
            {
                kplog.Info(refno + " - " + "Transaction is already paidout in ML-KP System!");
                return new TransResponse { respcode = "0", respmsg = "Transaction is already paidout in ML-KP System!" };
            }

            Int16 checkprefresp = checkprefund(accountid, principal, currency);
            if (checkprefresp != 0)
            {
                kplog.Info(refno + " - " + getErrorMessage(checkprefresp));
                return new TransResponse { respcode = "0", respmsg = getErrorMessage(checkprefresp) };
            }

            if (refno.Equals("") || refno.Equals(null) || bcode.Equals("") || bcode.Equals(null))
            {
                kplog.Info(refno + " - " + "Required Field is empty");
                return new TransResponse { respcode = "0", respmsg = "Required Field is empty" };
            }

            #region commitToPartners
            //commit to partners API here....
            Boolean iscommited = false;
            String commitResponse = "test to success";
            Int16 remark = 0;
            using (MySqlConnection cons = partnerNEW_ConDB.getConnection())
            {
                cons.Open();
                using (MySqlCommand datacommand = cons.CreateCommand())
                {
                    datacommand.CommandType = System.Data.CommandType.StoredProcedure;
                    datacommand.CommandText = "kpadminpartnerslog.api_insertpartnershistorylogs";
                    datacommand.Parameters.Clear();
                    //datacommand.Parameters.AddWithValue("Transdate", Claimeddt);
                    datacommand.Parameters.AddWithValue("_refno", refno);
                    datacommand.Parameters.AddWithValue("_partnerid", accountid);
                    datacommand.Parameters.AddWithValue("_currency", currency);
                    datacommand.Parameters.AddWithValue("_remarks", remark.ToString());
                    datapass = " - Error in saving partners history data - ";
                    datacommand.ExecuteNonQuery();
                }
                cons.Close();
            }

            //iscommited = true;

            //proceed saving to ML Database after committing to partners

            #endregion  commitToPartners

            string testForDuplicate = "";
            var attempt = 0;
            do
            {
                attempt++;
                string pPo = "";
                if (testForDuplicate == "DUPLICATE")
                {
                    kplog.Debug("testForDuplicate: persist - " + attempt);
                    for (int c = 0; c < 3; c++)
                    {
                        pPo = CheckControl(POControl, dt, bcode, zcode.ToString(), stationno, (isremote == false) ? 1 : 3);
                        if (pPo.StartsWith("FATAL"))
                        {

                        }
                        else { break; }
                    }
                    if (!pPo.ToUpper().StartsWith("FATAL"))
                    {
                        POControl = pPo;
                        series = pPo.Substring(POControl.Length - 6, 6);
                    }
                    else
                    {
                        kplog.Fatal(refno + " respCode: 0 respMsg: Transaction unable to proceeed due to payout saving error process errordetails: " + pPo);
                        return new TransResponse { respcode = "0", respmsg = "Transaction unable to proceeed due to payout saving error process" };
                    }
                }
                SOControl = "S" + POControl;
                kplog.Debug(refno + " - " + "SO control: " + SOControl);
                MySqlConnection conDB2 = partnerNEW_ConDB.getConnection(); // NEW DB
                MySqlConnection conDB1 = partnerOLD_ConDB.getConnection(); //OLD DB

                conDB2.Open();
                MySqlCommand cmdDB2 = conDB2.CreateCommand();
                MySqlTransaction transDB2 = conDB2.BeginTransaction(IsolationLevel.ReadCommitted);

                conDB1.Open();
                MySqlCommand cmdDB1 = conDB1.CreateCommand();
                MySqlTransaction transDB1 = conDB1.BeginTransaction(IsolationLevel.ReadCommitted);

                cmdDB1.Transaction = transDB1;
                cmdDB1.CommandText = "SET AUTOCOMMIT=0";
                cmdDB1.ExecuteNonQuery();

                cmdDB2.Transaction = transDB2;
                cmdDB2.CommandText = "SET AUTOCOMMIT=0";
                cmdDB2.ExecuteNonQuery();

                kplog.Info(refno + " - " + "Attempt count inserting ML Database: " + attempt);
                int sodb365, podb365, poso, soTransLogs, poTransLogs, deductBalance, corpoTrans, updateSeries;

                try
                {
                    //INSERTION OF DATA TO ML DATABASE

                    //Saving data to sendout 365 table
                    String month = dt.ToString("MM-dd").Replace("-", "");
                    kplog.Debug(" attempting to insert kppartners.sendout" + month + " referenceno: " + refno + " controlNo: " + SOControl);
                    cmdDB2.CommandText = "Insert into kppartners.sendout" + month + " (ControlNo, ReferenceNo, IRNo,Currency, Principal," +
                                  "Charge, OtherCharge, Total, CancelledDate, AccountCode, TransDate, CancelledByOperatorID," +
                                  "CancelledByBranchCode, CancelledByZoneCode, CancelledByStationID, CancelReason, CancelDetails, " +
                                  "SenderFName, SenderLName, SenderMName, SenderName, ReceiverFName, ReceiverLName, ReceiverMName, " +
                                  "ReceiverName, ReceiverAddress, ReceiverGender, ReceiverContactNo, ReceiverBirthDate, CancelCharge, " +
                                  "ChargeTo, Forex, Traceno, SenderAddress, SenderGender,SenderContactNo, sessionID, OtherDetails,OperatorID, " +
                                  "StationID, ReceiverStreet, ReceiverProvince, ReceiverCountry, KPTN, Message, Redeem, BranchCode, SenderBirthdate)" +
                                  "values(@ControlNo, @ReferenceNo, @IRNo,@Currency, @Principal," +
                                  "@Charge, @OtherCharge, @Total, @CancelledDate, @AccountCode, @TransDate, @CancelledByOperatorID," +
                                  "@CancelledByBranchCode, @CancelledByZoneCode, @CancelledByStationID, @CancelReason, @CancelDetails, " +
                                  "@SenderFName, @SenderLName, @SenderMName, @SenderName, @ReceiverFName, @ReceiverLName, @ReceiverMName, " +
                                  "@ReceiverName, @ReceiverAddress, @ReceiverGender, @ReceiverContactNo, @ReceiverBirthDate, @CancelCharge, " +
                                  "@ChargeTo, @Forex, @Traceno, @SenderAddress, @SenderGender,@SenderContactNo, @sessionID, @OtherDetails, @OperatorID, " +
                                  "@StationID, @ReceiverStreet, @ReceiverProvince, @ReceiverCountry, @KPTN, @Message, @Redeem, @BranchCode, @SenderBirthdate);";
                    //cmdDB2.CommandType = CommandType.Text;
                    cmdDB2.Parameters.Clear();
                    cmdDB2.Parameters.AddWithValue("ControlNo", SOControl);
                    cmdDB2.Parameters.AddWithValue("ReferenceNo", refno);
                    cmdDB2.Parameters.AddWithValue("IRNo", "");
                    cmdDB2.Parameters.AddWithValue("Currency", currency);
                    cmdDB2.Parameters.AddWithValue("Principal", principal);
                    cmdDB2.Parameters.AddWithValue("Charge", chargetotal);
                    cmdDB2.Parameters.AddWithValue("OtherCharge", 0);
                    cmdDB2.Parameters.AddWithValue("Total", principal + chargetotal);
                    cmdDB2.Parameters.AddWithValue("CancelledDate", "0000-00-00");
                    cmdDB2.Parameters.AddWithValue("AccountCode", accountid);
                    cmdDB2.Parameters.AddWithValue("TransDate", dt);
                    cmdDB2.Parameters.AddWithValue("CancelledByOperatorID", "");
                    cmdDB2.Parameters.AddWithValue("CancelledByBranchCode", "");
                    cmdDB2.Parameters.AddWithValue("CancelledByZoneCode", "");
                    cmdDB2.Parameters.AddWithValue("CancelledByStationID", "");
                    cmdDB2.Parameters.AddWithValue("CancelReason", "");
                    cmdDB2.Parameters.AddWithValue("CancelDetails", "");
                    cmdDB2.Parameters.AddWithValue("SenderFName", "");
                    cmdDB2.Parameters.AddWithValue("SenderLName", "");
                    cmdDB2.Parameters.AddWithValue("SenderMName", "");
                    cmdDB2.Parameters.AddWithValue("SenderName", sendername);
                    cmdDB2.Parameters.AddWithValue("ReceiverFName", "");
                    cmdDB2.Parameters.AddWithValue("ReceiverLName", "");
                    cmdDB2.Parameters.AddWithValue("ReceiverMName", "");
                    cmdDB2.Parameters.AddWithValue("ReceiverName", receivername);
                    cmdDB2.Parameters.AddWithValue("ReceiverAddress", receiveraddress);
                    cmdDB2.Parameters.AddWithValue("ReceiverGender", "");
                    cmdDB2.Parameters.AddWithValue("ReceiverContactNo", receivercontact);
                    cmdDB2.Parameters.AddWithValue("ReceiverBirthDate", "");
                    cmdDB2.Parameters.AddWithValue("CancelCharge", 0);
                    cmdDB2.Parameters.AddWithValue("ChargeTo", "Partner");
                    cmdDB2.Parameters.AddWithValue("Forex", 0);
                    cmdDB2.Parameters.AddWithValue("TraceNo", kptn);
                    cmdDB2.Parameters.AddWithValue("SenderAddress", "");
                    cmdDB2.Parameters.AddWithValue("SenderGender", "");
                    cmdDB2.Parameters.AddWithValue("SenderContactNo", "");
                    cmdDB2.Parameters.AddWithValue("sessionID", sessionid);
                    cmdDB2.Parameters.AddWithValue("OtherDetails", "");
                    cmdDB2.Parameters.AddWithValue("OperatorID", userID);
                    cmdDB2.Parameters.AddWithValue("StationID", stationno);
                    cmdDB2.Parameters.AddWithValue("ReceiverStreet", "");
                    cmdDB2.Parameters.AddWithValue("ReceiverProvince", "");
                    cmdDB2.Parameters.AddWithValue("ReceiverCountry", "");
                    cmdDB2.Parameters.AddWithValue("KPTN", kptn);
                    cmdDB2.Parameters.AddWithValue("Message", "");
                    cmdDB2.Parameters.AddWithValue("Redeem", 0);
                    cmdDB2.Parameters.AddWithValue("BranchCode", bcode);
                    cmdDB2.Parameters.AddWithValue("SenderBirthdate", "");
                    datapass = "Sendout table ";
                    sodb365 = cmdDB2.ExecuteNonQuery();

                    kplog.Debug(" attempting to insert kppartners.payout" + month + " referenceno: " + refno + " controlNo: " + POControl);
                    //Saving data to payout 365 table
                    cmdDB2.CommandText = "Insert into kppartners.payout" + month + "(ControlNo, ReferenceNo, ClaimedDate, OperatorID, " +
                                  "StationID, IRNo, IsRemote, RemoteBranch, RemoteOperatorID, Reason, AccountCode, Currency, " +
                                  "Principal, Forex, Relation, IDType, IDNo, ExpiryDate, BranchCode, ZoneCode, CancelledDate," +
                                  "CancelledByOperatorID,CancelledByStationID, CancelledType, CancelledReason, CancelledCustCharge, CancelledByBranchCode, " +
                                  "ReceiverFName, ReceiverLName, ReceiverMName, ReceiverName, ReceiverAddress, ReceiverGender, " +
                                  "ReceiverContactNo, ReceiverBirthdate, CancelledEmpCharge, Balance, DormantCharge, ServiceCharge, " +
                                  "RemoteZoneCode, SenderFName, SenderLName, SenderMName, SenderName, CancelledByZoneCode, SenderAddress, " +
                                  "SenderContactNo, SenderGender, Traceno, SessionID, ReceiverStreet, ReceiverProvince, ReceiverCountry, KPTN)" +
                                  "values(@ControlNo, @ReferenceNo, @ClaimedDate, @OperatorID, " +
                                  "@StationID, @IRNo, @IsRemote, @RemoteBranch, @RemoteOperatorID, @Reason, @AccountCode, @Currency, " +
                                  "@Principal, @Forex, @Relation, @IDType, @IDNo, @ExpiryDate, @BranchCode, @ZoneCode, @CancelledDate," +
                                  "@CancelledByOperatorID, @CancelledByStationID, @CancelledType, @CancelledReason, @CancelledCustCharge, @CancelledByBranchCode, " +
                                  "@ReceiverFName, @ReceiverLName, @ReceiverMName, @ReceiverName, @ReceiverAddress, @ReceiverGender, " +
                                  "@ReceiverContactNo, @ReceiverBirthdate, @CancelledEmpCharge, @Balance, @DormantCharge, @ServiceCharge, " +
                                  "@RemoteZoneCode, @SenderFName, @SenderLName, @SenderMName, @SenderName, @CancelledByZoneCode, @SenderAddress, " +
                                  "@SenderContactNo, @SenderGender, @Traceno, @SessionID, @ReceiverStreet, @ReceiverProvince, @ReceiverCountry, @KPTN);";
                    //cmdDB2.CommandType = CommandType.Text;
                    cmdDB2.Parameters.Clear();
                    cmdDB2.Parameters.AddWithValue("ControlNo", POControl);
                    cmdDB2.Parameters.AddWithValue("ReferenceNo", refno);
                    cmdDB2.Parameters.AddWithValue("ClaimedDate", dt);
                    cmdDB2.Parameters.AddWithValue("OperatorID", userID);
                    cmdDB2.Parameters.AddWithValue("StationID", stationno);
                    cmdDB2.Parameters.AddWithValue("IRNo", "");
                    cmdDB2.Parameters.AddWithValue("IsRemote", Convert.ToInt32(isremote));
                    cmdDB2.Parameters.AddWithValue("RemoteBranch", remotebcode);
                    cmdDB2.Parameters.AddWithValue("RemoteOperatorID", remoteOperator);
                    cmdDB2.Parameters.AddWithValue("Reason", "");
                    cmdDB2.Parameters.AddWithValue("AccountCode", accountid);
                    cmdDB2.Parameters.AddWithValue("Currency", currency);
                    cmdDB2.Parameters.AddWithValue("Principal", principal);
                    cmdDB2.Parameters.AddWithValue("Forex", 0);
                    cmdDB2.Parameters.AddWithValue("Relation", "");
                    cmdDB2.Parameters.AddWithValue("IDType", ben_IDType);
                    cmdDB2.Parameters.AddWithValue("IDNo", ben_IDNo);
                    cmdDB2.Parameters.AddWithValue("ExpiryDate", expirydate);
                    cmdDB2.Parameters.AddWithValue("BranchCode", bcode);
                    cmdDB2.Parameters.AddWithValue("ZoneCode", zcode);
                    cmdDB2.Parameters.AddWithValue("CancelledDate", "0000-00-00");
                    cmdDB2.Parameters.AddWithValue("CancelledByOperatorID", "");
                    cmdDB2.Parameters.AddWithValue("CancelledByStationID", "");
                    cmdDB2.Parameters.AddWithValue("CancelledType", "");
                    cmdDB2.Parameters.AddWithValue("CancelledReason", "");
                    cmdDB2.Parameters.AddWithValue("CancelledCustCharge", 0);
                    cmdDB2.Parameters.AddWithValue("CancelledByBranchCode", "");
                    cmdDB2.Parameters.AddWithValue("ReceiverFName", "");
                    cmdDB2.Parameters.AddWithValue("ReceiverLName", "");
                    cmdDB2.Parameters.AddWithValue("ReceiverMName", "");
                    cmdDB2.Parameters.AddWithValue("ReceiverName", receivername);
                    cmdDB2.Parameters.AddWithValue("ReceiverAddress", receiveraddress);
                    cmdDB2.Parameters.AddWithValue("ReceiverGender", "");
                    cmdDB2.Parameters.AddWithValue("ReceiverContactNo", receivercontact);
                    cmdDB2.Parameters.AddWithValue("ReceiverBirthDate", "");
                    cmdDB2.Parameters.AddWithValue("CancelledEmpCharge", 0);
                    cmdDB2.Parameters.AddWithValue("Balance", 0);
                    cmdDB2.Parameters.AddWithValue("Dormantcharge", 0);
                    cmdDB2.Parameters.AddWithValue("ServiceCharge", 0);
                    cmdDB2.Parameters.AddWithValue("RemoteZoneCode", remotezcode);
                    cmdDB2.Parameters.AddWithValue("SenderFName", "");
                    cmdDB2.Parameters.AddWithValue("SenderLName", "");
                    cmdDB2.Parameters.AddWithValue("SenderMName", "");
                    cmdDB2.Parameters.AddWithValue("SenderName", sendername);
                    cmdDB2.Parameters.AddWithValue("CancelledByZoneCode", "");
                    cmdDB2.Parameters.AddWithValue("SenderAddress", "");
                    cmdDB2.Parameters.AddWithValue("SenderContactNo", "");
                    cmdDB2.Parameters.AddWithValue("SenderGender", "");
                    cmdDB2.Parameters.AddWithValue("TraceNo", kptn);
                    cmdDB2.Parameters.AddWithValue("SessionID", sessionid + "|" + receiptno);
                    cmdDB2.Parameters.AddWithValue("ReceiverStreet", "");
                    cmdDB2.Parameters.AddWithValue("ReceiverProvince", "");
                    cmdDB2.Parameters.AddWithValue("ReceiverCountry", "");
                    cmdDB2.Parameters.AddWithValue("KPTN", kptn);
                    datapass = "Payout table ";
                    podb365 = cmdDB2.ExecuteNonQuery();

                    kplog.Debug(" attempting to insert kppartners.sotxnref and kppartners.potxnref referenceno: " + refno + " controlNo: " + POControl);
                    //Saving data to sendout and payout transaction reference logs
                    cmdDB2.CommandText = "`kppartners`.`insertPOSO`";
                    String reftable = "sendout" + month;
                    String reftable2 = "payout" + month;
                    cmdDB2.CommandType = CommandType.StoredProcedure;
                    cmdDB2.Parameters.Clear();
                    cmdDB2.Parameters.AddWithValue("_RefNum", refno);
                    cmdDB2.Parameters.AddWithValue("_claimedDate", dt);
                    cmdDB2.Parameters.AddWithValue("_CancelledDate", "0000-00-00");
                    cmdDB2.Parameters.AddWithValue("_RefTableSO", reftable);
                    cmdDB2.Parameters.AddWithValue("_RefTablePO", reftable2);
                    cmdDB2.Parameters.AddWithValue("_RefAccntCode", accountid);
                    cmdDB2.Parameters.AddWithValue("_BatchNum", 1);
                    cmdDB2.Parameters.AddWithValue("_currency", currency);
                    cmdDB2.Parameters.AddWithValue("_transtype", 1);
                    datapass = "sendout/payout transaction reference table ";
                    poso = cmdDB2.ExecuteNonQuery();

                    //Saving data to sendout transaction logs
                    cmdDB2.CommandText = "kpadminpartnerslog.savepartnersLog";
                    cmdDB2.CommandType = CommandType.StoredProcedure;
                    cmdDB2.Parameters.Clear();
                    cmdDB2.Parameters.AddWithValue("refno", refno);
                    cmdDB2.Parameters.AddWithValue("kptnno", kptn);
                    cmdDB2.Parameters.AddWithValue("accountcode", accountid);
                    cmdDB2.Parameters.AddWithValue("currency", currency);
                    cmdDB2.Parameters.AddWithValue("action", "SENDOUT");
                    cmdDB2.Parameters.AddWithValue("isremote", Convert.ToInt32(isremote));
                    cmdDB2.Parameters.AddWithValue("txndate", dt);
                    cmdDB2.Parameters.AddWithValue("stationcode", DBNull.Value);
                    cmdDB2.Parameters.AddWithValue("stationno", stationno);
                    cmdDB2.Parameters.AddWithValue("zonecode", zcode);
                    cmdDB2.Parameters.AddWithValue("branchcode", "API");
                    cmdDB2.Parameters.AddWithValue("operatorid", userID);
                    cmdDB2.Parameters.AddWithValue("remotebranchcode", "API");
                    cmdDB2.Parameters.AddWithValue("remoteoperator", remoteOperator);
                    cmdDB2.Parameters.AddWithValue("remotezonecode", remotezcode);
                    cmdDB2.Parameters.AddWithValue("cancelledreason", DBNull.Value);
                    cmdDB2.Parameters.AddWithValue("remotereason", DBNull.Value);
                    cmdDB2.Parameters.AddWithValue("oldkptnno", DBNull.Value);
                    cmdDB2.Parameters.AddWithValue("type", "kppartners");
                    cmdDB2.Parameters.AddWithValue("sendname", sendername);
                    cmdDB2.Parameters.AddWithValue("recname", receivername);
                    cmdDB2.Parameters.AddWithValue("principal", principal);
                    datapass = "SendoutTransactionslog table  ";
                    soTransLogs = cmdDB2.ExecuteNonQuery();
                    //Done Saving data to sendout transaction logs

                    //Saving data to payout transaction logs
                    cmdDB2.CommandText = "kpadminpartnerslog.savepartnersLog";
                    cmdDB2.CommandType = CommandType.StoredProcedure;
                    cmdDB2.Parameters.Clear();
                    cmdDB2.Parameters.AddWithValue("refno", refno);
                    cmdDB2.Parameters.AddWithValue("kptnno", kptn);
                    cmdDB2.Parameters.AddWithValue("accountcode", accountid);
                    cmdDB2.Parameters.AddWithValue("currency", currency);
                    cmdDB2.Parameters.AddWithValue("action", "PAYOUT");
                    cmdDB2.Parameters.AddWithValue("isremote", Convert.ToInt32(isremote));
                    cmdDB2.Parameters.AddWithValue("txndate", dt);
                    cmdDB2.Parameters.AddWithValue("stationcode", DBNull.Value);
                    cmdDB2.Parameters.AddWithValue("stationno", stationno);
                    cmdDB2.Parameters.AddWithValue("zonecode", zcode);
                    cmdDB2.Parameters.AddWithValue("branchcode", bcode);
                    cmdDB2.Parameters.AddWithValue("operatorid", userID);
                    cmdDB2.Parameters.AddWithValue("remotebranchcode", remotebcode);
                    cmdDB2.Parameters.AddWithValue("remoteoperator", remoteOperator);
                    cmdDB2.Parameters.AddWithValue("remotezonecode", remotezcode);
                    cmdDB2.Parameters.AddWithValue("cancelledreason", DBNull.Value);
                    cmdDB2.Parameters.AddWithValue("remotereason", DBNull.Value);
                    cmdDB2.Parameters.AddWithValue("oldkptnno", DBNull.Value);
                    cmdDB2.Parameters.AddWithValue("type", "kppartners");
                    cmdDB2.Parameters.AddWithValue("sendname", sendername);
                    cmdDB2.Parameters.AddWithValue("recname", receivername);
                    cmdDB2.Parameters.AddWithValue("principal", principal);
                    datapass = "PayoutTransactionslog table  ";
                    poTransLogs = cmdDB2.ExecuteNonQuery();
                    //Done Saving data to payout transaction logs

                    kplog.Debug(" attempting to insert kppartnerstransactions.corporatesendouts and corporatepayouts - referenceno: " + refno + " controlNo: " + POControl);
                    //Saving data to kppartnerstransaction payout logs
                    cmdDB2.CommandText = "`kppartnerstransactions`.`insertcorporatesendoutpayout101`";
                    cmdDB2.CommandType = CommandType.StoredProcedure;
                    cmdDB2.Parameters.Clear();
                    cmdDB2.Parameters.AddWithValue("_SOControlno", SOControl);
                    cmdDB2.Parameters.AddWithValue("_POControlno", POControl);
                    cmdDB2.Parameters.AddWithValue("_Kptn", kptn);
                    cmdDB2.Parameters.AddWithValue("_Refno", refno);
                    cmdDB2.Parameters.AddWithValue("_AccountID", accountid);
                    cmdDB2.Parameters.AddWithValue("_Currency", currency);
                    cmdDB2.Parameters.AddWithValue("_Transdate", dt);
                    cmdDB2.Parameters.AddWithValue("_StationNo", stationno);
                    cmdDB2.Parameters.AddWithValue("_IsRemote", Convert.ToInt32(isremote));
                    cmdDB2.Parameters.AddWithValue("_OperatorId", userID);
                    cmdDB2.Parameters.AddWithValue("_BranchCode", bcode);
                    cmdDB2.Parameters.AddWithValue("_Zonecode", zcode);
                    cmdDB2.Parameters.AddWithValue("_RemoteOperatorID", remoteOperator);
                    cmdDB2.Parameters.AddWithValue("_RemoteBranchcode", remotebcode);
                    cmdDB2.Parameters.AddWithValue("_RemoteZonecode", remotezcode);
                    cmdDB2.Parameters.AddWithValue("_Chargeamount", chargetotal);
                    cmdDB2.Parameters.AddWithValue("_Principal", principal);
                    cmdDB2.Parameters.AddWithValue("_Total", principal + chargetotal);
                    cmdDB2.Parameters.AddWithValue("_TranceNo", kptn);
                    cmdDB2.Parameters.AddWithValue("_SessionID", sessionid);
                    cmdDB2.Parameters.AddWithValue("_ReceiverFname", receivername);
                    cmdDB2.Parameters.AddWithValue("_ReceiverLname", "");
                    cmdDB2.Parameters.AddWithValue("_ReceiverMname", "");
                    cmdDB2.Parameters.AddWithValue("_senderfname", sendername);
                    cmdDB2.Parameters.AddWithValue("_senderlname", "");
                    cmdDB2.Parameters.AddWithValue("_sendermname", "");
                    datapass = "corporatesendouts and corporatepayouts table  ";
                    corpoTrans = cmdDB2.ExecuteNonQuery();
                    //Done Saving data to kppartnerstransaction payout logs

                    //Update corporate prefund balance
                    cmdDB2.CommandType = CommandType.StoredProcedure;
                    cmdDB2.CommandText = "kpadminpartners.deductRunningBalance";
                    cmdDB2.Parameters.Clear();
                    cmdDB2.Parameters.AddWithValue("_refno", refno);
                    cmdDB2.Parameters.AddWithValue("_accountid", accountid);
                    cmdDB2.Parameters.AddWithValue("_currency", currency);
                    cmdDB2.Parameters.AddWithValue("_runningbalance", totalrunningbalance);
                    cmdDB2.Parameters.AddWithValue("_principal", principal);
                    cmdDB2.Parameters.AddWithValue("_charge", chargetotal);
                    datapass = "prefund balance ";
                    deductBalance = cmdDB2.ExecuteNonQuery();

                    //Update control series for payout
                    Int32 sr = Convert.ToInt32(series);
                    if (!isremote)
                    {
                        cmdDB1.CommandText = "`kpadminpartners`.`updatecontrolseries`";
                        cmdDB1.CommandType = CommandType.StoredProcedure;
                        cmdDB1.Parameters.Clear();
                        cmdDB1.Parameters.AddWithValue("_seriesnum", sr + 1);
                        cmdDB1.Parameters.AddWithValue("_stationno", stationno);
                        cmdDB1.Parameters.AddWithValue("_bcode", bcode);
                        cmdDB1.Parameters.AddWithValue("_zcode", zcode);
                        cmdDB1.Parameters.AddWithValue("_type", 1);
                        datapass = "control series payout  ";
                        updateSeries = cmdDB1.ExecuteNonQuery();
                    }
                    else
                    {
                        cmdDB1.CommandText = "`kpadminpartners`.`updatecontrolseries`";
                        cmdDB1.CommandType = CommandType.StoredProcedure;
                        cmdDB1.Parameters.Clear();
                        cmdDB1.Parameters.AddWithValue("_seriesnum", sr + 1);
                        cmdDB1.Parameters.AddWithValue("_stationno", "01");
                        cmdDB1.Parameters.AddWithValue("_bcode", remotebcode);
                        cmdDB1.Parameters.AddWithValue("_zcode", remotezcode);
                        cmdDB1.Parameters.AddWithValue("_type", 3);
                        datapass = "control series payout  ";
                        updateSeries = cmdDB1.ExecuteNonQuery();
                    }

                    if ((sodb365 < 1) || (podb365 < 1) || (poso < 1) || (soTransLogs < 1) || (poTransLogs < 1) ||
                        (deductBalance < 1) || (corpoTrans < 1) || (updateSeries < 1))
                    {
                        transDB2.Rollback();
                        conDB2.Close();
                        transDB1.Rollback();
                        conDB1.Close();
                    }
                    else
                    {
                        //commitResponse = commitToPartners(refno, kptn);
                        //commitResponse = "SUCCESS";
                        if (commitResponse.ToUpper() == "SUCCESS")
                        {
                            kplog.Info("Successfully committed to partners -  refno: " + refno + " sendername: " + sendername + " receivername: " + receivername +
                                   " receiveraddress: " + receiveraddress + " receivercontact: " + receivercontact + "currency: " + currency + " FXAmount: " + FXAmount +
                                   " transpin: " + transpin + " sec_Token: " + sec_Token + " series: " + series + " isremote: " + isremote + " remoteOperator: " + remoteOperator +
                                   " remotebcode: " + remotebcode + " remotezcode: " + remotezcode + " remotereason: " + remotereason + " bcode: " + bcode + " zcode: " + zcode +
                                   " operatorID:" + operatorID + " stationno:" + stationno + " ben_IDType:" + ben_IDType + " ben_IDNo:" + ben_IDNo + "expirydate:" + expirydate +
                                   " transtype:" + transtype + " POControl:" + POControl + " kptn: " + kptn);
                            iscommited = true;
                            remark = 1;
                        }

                        if (!iscommited) //trapping for uncommitted transaction
                        {
                            transDB1.Rollback();
                            transDB2.Rollback();
                            conDB1.Close();
                            conDB2.Close();
                            kplog.Fatal(refno + " - ERROR DURING TRANSACTION COMMIT: " + commitResponse);
                            if (commitResponse.ToUpper().Contains("TIMEOUT"))
                            {
                                kplog.Fatal(refno + " - " + bcode + " - " + "ERROR DURING TRANSACTION COMMIT: Problem on partner. " + commitResponse);
                                return new TransResponse { respcode = "0", respmsg = "Problem on partner. Kindly re-process transaction." };
                            }
                            else
                            {
                                kplog.Fatal(refno + " - " + bcode + " - " + "ERROR DURING TRANSACTION COMMIT: " + commitResponse);
                                return new TransResponse { respcode = "0", respmsg = "Error during saving of transaction. Please try again later" };
                            }
                        }
                        else
                        {
                            transDB2.Commit();
                            conDB2.Close();
                            transDB1.Commit();
                            conDB1.Close();
                            transdetails.respcode = "1";
                            transdetails.respmsg = "Transaction successfully saved.";
                            transdetails.transdate = dt.ToString("yyyy-MM-dd HH:mm:ss");
                            kplog.Info(refno + " - " + " Transactions response code " + transdetails.respcode + " - " + transdetails.respmsg);
                            break;
                        }
                    }

                }
                catch (Exception ex)
                {
                    transDB2.Rollback();
                    transDB1.Rollback();
                    conDB2.Close();
                    conDB1.Close();
                    if (ex.Message.ToString().ToUpper().Contains("DUPLICATE ENTRY"))
                    {
                        testForDuplicate = "DUPLICATE";
                    }
                    kplog.Fatal(refno + " - " + bcode + " - " + "ERROR DURING TRANSACTION COMMIT: " + commitResponse + " attempt: " + attempt + " errordetails: " + ex.ToString());
                    //kplog.Fatal();
                    if (attempt > 3)
                    {
                        kplog.Fatal(refno + " - " + bcode + " - " + "ERROR DURING TRANSACTION COMMIT: " + commitResponse + " errordetails: " + ex.ToString());
                        return new TransResponse { respcode = "0", respmsg = "Error during saving of transaction. Please try again later" };
                    }
                }

                if (attempt > 3)
                {
                    kplog.Error(refno + " - " + bcode + " - " + "ERROR DURING TRANSACTION COMMIT: " + commitResponse + " errordetails: ");
                    return new TransResponse { respcode = "0", respmsg = "Error during saving of transaction. Please try again later" };
                }

            } while (true);

        }
        catch (TimeoutException ex)
        {
            transdetails.respcode = "0";
            transdetails.respmsg = "Unable to process request. Connection timeout occured. Please try again later.";
            kplog.Fatal(refno + " - " + ex.ToString());
        }
        catch (WebException ex)
        {
            transdetails.respcode = "0";
            transdetails.respmsg = "Unable to process request. Failed in connecting to partners API. Please try again later.";
            kplog.Fatal(refno + " - " + ex.ToString());
        }
        catch (Exception ex)
        {
            transdetails.respcode = "0";
            transdetails.respmsg = "Unable to process request. The system encountered some technical problem. Sorry for the inconvenience.";
            kplog.Fatal(refno + " - " + ex.ToString());
        }
        return transdetails;
    }
    public TransResponse retrieveCollection(String kptn) 
    {
        TransResponse trans = new TransResponse();

        String retcollectionURL = url + "/v1/cash-pickup/collections/" + kptn;
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(retcollectionURL);
        request.Method = "GET";
        request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(username + ":" + password));
        request.ContentType = "application/json;charset=UTF-8";
        request.PreAuthenticate = true;

        kplog.Info("TransferTo ShowremittanceDetails Request : [" + kptn + "] - " + request.RequestUri);

        WebResponse response = request.GetResponse();
        StreamReader stream = new StreamReader(response.GetResponseStream());
        var resp = stream.ReadToEnd().Trim();

        kplog.Info(" TransferTo complete collection request : " + kptn + " - " + resp);

        trans.respcode = "1";
        trans.respmsg = "success";
        trans.transdate = getserverdatePartners().ToString();
        trans.partnersdata = resp;
        return trans;
    }
    #region private method
     private Boolean InputVerfication(String refno, String branchcode, String stationID, String userid, String Pocontrol)
     {
         if (!refno.Equals("") && (!branchcode.Equals("") && (!stationID.Equals("") && (!userid.Equals("") && (!Pocontrol.Equals(""))))))
             return true;
         else
         {
             kplog.Error("Invalid Credentials");
             return false;
         }
     }
    private Boolean CheckDate(String refno)
    {
        try
        {

            using (MySqlConnection cons = partnerNEW_ConDB.getConnection())
            {
                cons.Open();
                using (MySqlCommand command = cons.CreateCommand())
                {
                    command.CommandText = "Select ClaimedDate,tablereference, CancelledDate from kppartners.potxnref where ReferenceNo = '" + refno + "' and AccountCode = '" + accountid + "' order by CancelledDate desc limit 1 ;";
                    command.CommandType = CommandType.Text;
                    using (MySqlDataReader Reader = command.ExecuteReader())
                    {
                        if (Reader.HasRows)
                        {
                            Reader.Read();

                            String Date = Reader["ClaimedDate"].ToString();
                            String CancelledDate = Reader["CancelledDate"].ToString();
                            String CancelledDates = Convert.ToString(CancelledDate).Substring(0, 8);
                            String Dates = Convert.ToString(Date).Substring(0, 8);
                            String tableref = Reader["tablereference"].ToString();
                            Reader.Close();
                            if (!Dates.Equals("0/0/0000") && (CancelledDates.Equals("0/0/0000")))
                            {
                                cons.Close();
                                return true;
                            }
                            else
                                cons.Close();
                            return false;

                        }
                        else
                            cons.Close();
                        return false;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            kplog.Fatal(ex.ToString());
            return false;
        }
    }
    private DateTime getserverdatePartners()
    {
        DateTime serverdate = Convert.ToDateTime("1986-05-29");
        using (MySqlConnection con = partnerNEW_ConDB.getConnection())
        {
            try
            {
                con.Open();
                using (MySqlCommand cmd = con.CreateCommand())
                {
                    string query = "select now() as serverdate";
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandText = query;
                    serverdate = Convert.ToDateTime(cmd.ExecuteScalar());
                    cmd.Dispose();
                }
                con.Close();
            }
            catch (Exception ex)
            {
                kplog.Fatal(ex.ToString());
                con.Close();
                throw new Exception(ex.Message);
            }
        }
        return serverdate;
    }
    private String generateKPTNpartners(string branchcode, Int16 zonecode)
    {
        try
        {
            DateTime dt = (DateTime)getserverdatePartners();
            jp.takel.PseudoRandom.MersenneTwister randGen = new jp.takel.PseudoRandom.MersenneTwister((uint)HiResDateTime.UtcNow.Ticks);
            return branchcode + dt.ToString("dd") + zonecode.ToString() + randGen.Next(1000000000, Int32.MaxValue).ToString() + dt.ToString("MM");
        }
        catch (Exception a)
        {
            kplog.Fatal(a.ToString());
            throw new Exception(a.ToString());
        }
    }
    private Boolean isMLpaidout(string refno, string accountid)
    {
        Boolean ispaidout = false;
        using (MySqlConnection con = partnerNEW_ConDB.getConnection())
        {
            try
            {
                con.Open();
                using (MySqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.CommandText = "kppartners.api_isMLpaidout";
                    cmd.Parameters.AddWithValue("_accountcode", accountid);
                    cmd.Parameters.AddWithValue("_referenceno", refno);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            ispaidout = reader.GetBoolean("ispaidout");
                            reader.Close();
                        }
                        else
                            ispaidout = false;

                        reader.Close();
                    }
                }
                con.Close();
            }
            catch (Exception ex)
            {
                kplog.Fatal(ex.ToString());
                con.Close();
                throw new Exception(ex.Message);
            }
        }
        return ispaidout;
    }
    private String getErrorMessage(Int16 prefcode)
    {
        String message = "";

        switch (prefcode)
        {
            case 0:
                message = "Success.";
                break;
            case 10:
                message = "Principal Amount is out of range in the charges bracketing!";
                break;
            case 11:
                message = "Principal Amount exceeds to the limit of maximum transaction amount!";
                break;
            case 12:
                message = "Unable to process transaction. Account has insufficient balance.";
                break;
            case 13:
                message = "Unable to process transaction. Accounts credit amount exceeds to its limit.";
                break;
            case 14:
                message = "Unable to process transaction. No Query applied for charge type \"KP Charges\".";
                break;
            case 15:
                message = "Unable to process transaction. Account is unable to get charges.";
                break;
            case 100:
                message = "Technical error occured. Sorry for the inconvenience. Please call KP-SUPPORT. \nThank You.";
                break;
            case 101:
                message = "Unable to process request. Account is not active.";
                break;
            case 102:
                message = "Unable to process request. Account does not exist.";
                break;
            default:
                message = "Unknown error encountered. Please re-check codes.";
                break;
        }

        return message;
    }
    private DateTime GetYesterday2(DateTime date)
    {
        return date.AddDays(-1);
    }
    private Boolean isSameMonthYesterday(DateTime date)
    {
        try
        {
            if (GetYesterday2(date).Month.Equals(date.Month))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.ToString());
        }
    }
    public generateControlResponsePO generateControl(String bcode, Int32 type, Int32 zcode, String stationid)
    {
        if (string.IsNullOrEmpty(stationid))
        {
            return new generateControlResponsePO { respcode = 0, message = "station id is empty" };
        }
        if (string.IsNullOrEmpty(bcode))
        {
            return new generateControlResponsePO { respcode = 0, message = "branchcode is empty" };
        }

        String control;
        //DBConnection pointDB = null;
        //Boolean isNewDB = false;
        kplog.Info("generateControlpayoutAPI branchcode:" + bcode + " |stationNumber:" + stationid +
            " |type:" + type + " |zoneCode:" + zcode);
        DateTime dt;
        try
        {
            MySqlTransaction trans = null;

            Boolean toUpdate = false;
            using (MySqlConnection conn = partnerNEW_ConDB.getConnection())
            {
                using (MySqlCommand command = conn.CreateCommand())
                {
                    conn.Open();
                    trans = conn.BeginTransaction(IsolationLevel.ReadCommitted);
                    command.Transaction = trans;

                    command.CommandText = "SELECT NOW() as svrdt";
                    using (MySqlDataReader rdr = command.ExecuteReader())
                    {
                        rdr.Read();
                        dt = rdr.GetDateTime("svrdt");
                        rdr.Close();
                    }
                    try
                    {
                        command.CommandText = "Select station, bcode, userid, nseries, zcode, type from kpadminpartners.control where station = @st and bcode = @bcode and zcode = @zcode and `type` = @tp order by nseries desc limit 1";
                        command.Parameters.AddWithValue("st", stationid);
                        command.Parameters.AddWithValue("bcode", bcode);
                        command.Parameters.AddWithValue("zcode", zcode);
                        command.Parameters.AddWithValue("tp", type);
                        MySqlDataReader Reader = command.ExecuteReader();

                        if (Reader.HasRows)
                        {
                            Reader.Read();
                            toUpdate = true;
                            if (type == 0)
                            {
                                control = "S0" + zcode.ToString() + "-" + stationid + "-" + bcode;
                            }
                            else if (type == 1)
                            {
                                control = "P0" + zcode.ToString() + "-" + stationid + "-" + bcode;
                            }
                            else if (type == 2)
                            {
                                control = "S0" + zcode.ToString() + "-" + stationid + "-R" + bcode;
                            }
                            else if (type == 3)
                            {
                                control = "P0" + zcode.ToString() + "-" + stationid + "-R" + bcode;
                            }
                            else
                            {
                                return new generateControlResponsePO { respcode = 0, message = "Invalid Type", controlno = "", nseries = "" };
                            }
                            Int64 series = Convert.ToInt64(Reader["nseries"].ToString());
                            String toDisp = series.ToString().PadLeft(6, '0');
                            String s = Reader["Station"].ToString();
                            String nseries = Reader["nseries"].ToString().PadLeft(6, '0');
                            Reader.Close();

                            Int64 seriescorp = 0;
                            command.CommandText = "select MAX(SUBSTRING(controlno,LENGTH(controlno)-5,LENGTH(controlno))) as MAXSERIES " +
                            " from kppartnerstransactions.corporatepayouts where zonecode=@zcode2 and branchcode=@bcode2 and stationno=@st2 and isremote='0'";
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("zcode2", zcode);
                            command.Parameters.AddWithValue("bcode2", bcode);
                            command.Parameters.AddWithValue("st2", stationid);
                            MySqlDataReader re = command.ExecuteReader();
                            if (re.HasRows)
                            {
                                re.Read();
                                seriescorp = String.IsNullOrEmpty(re["MAXSERIES"].ToString()) ? 0 : Convert.ToInt64(re["MAXSERIES"]) + 1;
                                re.Close();

                                if (isSameMonthYesterday(dt))
                                {
                                    kplog.Info("   generateControlPayout api - " + control + "-" + dt.ToString("MMyy") + "-" + seriescorp.ToString().PadLeft(6, '0'));
                                    return new generateControlResponsePO { respcode = 1, message = "success", controlno = control + "-" + dt.ToString("MMyy") + "-" + seriescorp.ToString().PadLeft(6, '0'), nseries = seriescorp.ToString().PadLeft(6, '0') };
                                }
                                else
                                {
                                    kplog.Info("   generateControlPayout api - " + control + "-" + dt.ToString("MMyy") + "-" + seriescorp.ToString().PadLeft(6, '0'));
                                    return new generateControlResponsePO { respcode = 1, message = getRespMessage(1), controlno = control + "-" + dt.ToString("MMyy") + "-" + "000001", nseries = "000001" };
                                }

                            } re.Close();

                            conn.Close();
                            if (isSameMonthYesterday(dt))
                            {
                                return new generateControlResponsePO { respcode = 1, message = getRespMessage(1), controlno = control + "-" + dt.ToString("MMyy") + "-" + toDisp, nseries = toDisp };
                            }
                            else
                            {
                                return new generateControlResponsePO { respcode = 1, message = getRespMessage(1), controlno = control + "-" + dt.ToString("MMyy") + "-" + "000001", nseries = "000001" };
                            }

                        }
                        else
                        {
                            Reader.Close();
                            command.CommandText = "Insert into kpadminpartners.control (`station`,`bcode`,`userid`,`nseries`,`zcode`, `type`) values (@station,@branchcode,@uid,1,@zonecode,@type)";
                            if (type == 0)
                            {
                                control = "S0" + zcode.ToString() + "-" + stationid + "-" + bcode;
                            }
                            else if (type == 1)
                            {
                                control = "P0" + zcode.ToString() + "-" + stationid + "-" + bcode;
                            }
                            else if (type == 2)
                            {
                                control = "S0" + zcode.ToString() + "-" + stationid + "-R" + bcode;
                            }
                            else if (type == 3)
                            {
                                control = "P0" + zcode.ToString() + "-" + stationid + "-R" + bcode;
                            }
                            else
                            {
                                return new generateControlResponsePO { respcode = 0, message = "Invalid Type", controlno = "", nseries = "" };
                            }
                            command.Parameters.AddWithValue("station", stationid);
                            command.Parameters.AddWithValue("branchcode", bcode);
                            command.Parameters.AddWithValue("uid", "test");
                            command.Parameters.AddWithValue("zonecode", zcode);
                            command.Parameters.AddWithValue("type", type);
                            int x = command.ExecuteNonQuery();
                            trans.Commit();
                            conn.Close();

                            return new generateControlResponsePO { respcode = 1, message = "success", controlno = control + "-" + dt.ToString("MMyy") + "-" + "000001", nseries = "000001" };
                        }
                    }
                    catch (MySqlException ex)
                    {
                        trans.Rollback();
                        conn.Close();
                        kplog.Fatal(ex.ToString());
                        return new generateControlResponsePO { respcode = 0, message = ex.ToString(), ErrorDetail = ex.ToString() };
                    }
                }
            }
        }
        catch (MySqlException ex)
        {
            partnerNEW_ConDB.CloseConnection();
            kplog.Fatal(ex.ToString());
            return new generateControlResponsePO { respcode = 0, message = ex.ToString(), ErrorDetail = ex.ToString() };
        }
        catch (Exception ex)
        {
            partnerNEW_ConDB.CloseConnection();
            kplog.Fatal(ex.ToString());

            return new generateControlResponsePO { respcode = 0, message = ex.ToString(), ErrorDetail = ex.ToString() };
        }
    }
    private Double getCharge(double amount, string accountid, string currency)
    {
        DateTime datetoday = getserverdatePartners();
        Double chargeamount = 0.00;

        using (MySqlConnection con = partnerNEW_ConDB.getConnection())
        {
            try
            {
                con.Open();
                String chargetype = "", brackettiercode = "";
                Boolean isactive = false, exist = false;
                Double charge = 0.00;
                using (MySqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.CommandText = "kpadminpartners.api_getChargedetail";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("_accountid", accountid);
                    cmd.Parameters.AddWithValue("_currency", currency);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            chargetype = reader.GetString("chargetype");
                            charge = reader.GetDouble("chargeamount");
                            brackettiercode = reader["brackettiercode"].ToString();//reader.GetString("brackettiercode");
                            isactive = reader.GetBoolean("isactive");
                            exist = true;
                            reader.Close();
                        }
                        else
                        {
                            exist = false;
                        }
                        reader.Close();
                    }
                    cmd.Dispose();

                    switch (chargetype.ToUpper())
                    {
                        case "FIXED PRICE":
                            chargeamount = charge;
                            break;
                        case "FREE OF CHARGE":
                            chargeamount = 0.00;
                            break;
                        case "TIER BRACKET":
                            {
                                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                                cmd.CommandText = "kpadminpartners.api_getTierCharge";
                                cmd.Parameters.Clear();
                                cmd.Parameters.AddWithValue("_tiercode", brackettiercode);
                                cmd.Parameters.AddWithValue("_amount", amount);
                                Double chargeget = Convert.ToDouble(cmd.ExecuteScalar());
                                if (chargeget == 0.00)
                                    chargeamount = 888888.88; //indicator for out of range
                                else
                                    chargeamount = chargeget;
                                break;
                            }
                        case "KP CHARGES":
                            chargeamount = 777777.77;
                            //throw new Exception("No Query applied for charge type \"KP Charges\" ");
                            break;
                        default:
                            chargeamount = 666666.66; //indicator for no chargetype
                            break;
                    }
                    if (exist)
                    {
                        if (isactive == false)
                            chargeamount = 999999.99; //indicator for inactive 
                    }
                    else
                        chargeamount = 666666.66; //indicator for non exist

                }
                con.Close();
            }
            catch (Exception ex)
            {
                kplog.Fatal(ex.ToString());
                con.Close();
                throw new Exception(ex.Message);
            }
        }
        return chargeamount;
    }
    private String getInquireResp(Int32 code)
    {
        String x = "System Error";
        switch (code)
        {
            case 0:
                return x = "success";
            case 1:
                return x = "invalid traceno (initial TagAsCompleted request may have not been received)";
            case 3:
                return x = "cannot be processed due to other errors + error message";
            case 4:
                return x = "transaction does not exist";
            case 5:
                return x = "incorrect username or password";
            case 6:
                return x = "incorrect signature";
            default:
                return x;
        }
    }
    private Int16 checkprefund(string accountid, double principalamnt, string currency)
    {
        Int16 prefundcode = 0;
        Double charge = getCharge(principalamnt, accountid, currency);
        if (charge == 888888.88)
            return 10;
        if (charge == 777777.77)
            return 14;
        if (charge == 666666.66)
            return 15;
        using (MySqlConnection con = partnerNEW_ConDB.getConnection())
        {
            try
            {
                con.Open();
                String chargetype = "", chargeto = "", brackettiercode = "";
                Boolean isactive = false, creditactivation = false, exist = false;
                Double thresholdamount = 0.00, creditlimit = 0.00, runningbalance = 0.00, chargeamount = 0.00;
                using (MySqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.CommandText = "kpadminpartners.api_getChargedetail";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("_accountid", accountid);
                    cmd.Parameters.AddWithValue("_currency", currency);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            thresholdamount = reader.GetDouble("thresholdamount");
                            creditlimit = reader.GetDouble("creditlimit");
                            creditactivation = reader.GetBoolean("creditactivation");
                            runningbalance = reader.GetDouble("runningbalance");
                            brackettiercode = reader["brackettiercode"].ToString(); //reader.GetString("brackettiercode");
                            chargeto = reader.GetString("chargeto");
                            chargeamount = reader.GetDouble("chargeamount");
                            chargetype = reader.GetString("chargetype");
                            isactive = reader.GetBoolean("isactive");
                            exist = true;
                            reader.Close();

                            //added for the charge
                            //charge = chargeamount;
                        }
                        else
                        {
                            exist = false;
                        }
                        reader.Close();
                    }
                    cmd.Dispose();
                    if (exist)
                    {
                        if (isactive == false)
                            return 101; //indicator for inactive
                    }
                    else
                        return 102; //indicator for not exist


                    if (chargetype.ToUpper() != "TIER BRACKET")
                    {
                        if (thresholdamount < (principalamnt + charge))
                            return 11;
                    }
                    if (runningbalance < (principalamnt + charge))
                    {
                        if (creditactivation == false)
                            return 12;
                        else
                        {
                            if ((creditlimit + runningbalance) < (principalamnt + charge))
                                return 13;
                        }
                    }
                   
                }
                con.Close();
            }
            catch (Exception ex)
            {
                kplog.Fatal(ex.ToString());
                con.Close();
                throw new Exception(ex.Message);
            }
        }
        chargetotal = charge;
        return prefundcode;
    }
    public String CheckControl(string controlno, DateTime dt, string bcode, string zcode, string stationno, int type)
    {
        try
        {
            kplog.Info(" controlno: " + controlno + " dt: " + dt + " bcode: " + bcode + " zcode: " + zcode +
                " stationno: " + stationno + " type: " + type);

            Int32 latestSeries = 0;
            using (MySqlConnection con = partnerNEW_ConDB.getConnection())
            {
                con.Open();
                using (MySqlCommand cmd = con.CreateCommand())
                {
                    MySqlTransaction trans = con.BeginTransaction(IsolationLevel.ReadCommitted);
                    cmd.Transaction = trans;
                    cmd.CommandText = "set autocommit=0";
                    cmd.ExecuteNonQuery();
                    try
                    {
                        cmd.CommandText = "select ControlNo from kppartnerstransactions.corporatepayouts where ControlNo=@ControlNo";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("ControlNo", controlno);
                        MySqlDataReader reader = cmd.ExecuteReader();
                        if (reader.HasRows)
                        {
                            reader.Close();

                            cmd.CommandText = "SELECT MAX(SUBSTRING(controlno,LENGTH(controlno)-5,LENGTH(controlno))) AS max FROM kppartnerstransactions.corporatepayouts " +
                            "WHERE branchcode=@BranchCode AND stationno=@StationID AND zonecode=@ZoneCode";
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("BranchCode", bcode);
                            cmd.Parameters.AddWithValue("ZoneCode", Convert.ToInt32(zcode));
                            cmd.Parameters.AddWithValue("StationID", stationno);
                            MySqlDataReader rdrControl = cmd.ExecuteReader();
                            if (rdrControl.Read())
                            {
                                latestSeries = String.IsNullOrEmpty(rdrControl["max"].ToString()) ? 0 : Convert.ToInt32(rdrControl["max"].ToString());
                            }
                            rdrControl.Close();

                            cmd.CommandText = "update kpadminpartners.control set nseries=@nseries where bcode=@bcode and zcode=@zcode and type=@type and zcode=@zcode and station=@station";
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("nseries", latestSeries + 1);
                            cmd.Parameters.AddWithValue("station", stationno);
                            cmd.Parameters.AddWithValue("bcode", bcode);
                            cmd.Parameters.AddWithValue("zcode", Convert.ToInt32(zcode));
                            cmd.Parameters.AddWithValue("type", type);
                            cmd.ExecuteNonQuery();

                            trans.Commit();

                            kplog.Info("controlno: " + controlno + " - new controlno: " + "PO" + zcode + "-00-" + bcode + "-" + dt.ToString("yy") + "-" + (latestSeries + 1).ToString().PadLeft(6, '0'));
                            controlno = "PO" + zcode + "-00-" + bcode + "-" + dt.ToString("MMyy") + "-" + (latestSeries + 1).ToString().PadLeft(6, '0');
                        }
                        reader.Close();
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        kplog.Fatal(" - " + ex);
                        return "fatal: " + ex;
                    }
                }
                con.Close();
            }

            return controlno;
        }
        catch (Exception ex)
        {
            kplog.Fatal(" - " + ex);
            return "fatal: " + ex;
        }
    }
    private String getRespMessage(Int32 code)
    {
        String x = "System Error";
        switch (code)
        {
            case 0:
                return x = "Success";
            case 1:
                return x = "Transaction is Already PAID";
            case 2:
                return x = "claimed by another transaction";
            case 3:
                return x = "cannot be processed due to other errors";
            case 4:
                return x = "transaction does not exist";
            case 5:
                return x = "incorrect username or password";
            case 6:
                return x = "incorrect signature";
            case 7:
                return x = "Currency cannot be null.";
            case 8:
                return x = "Unable to process transaction. Amount exceeds to maximum limit";
            case 9:
                return x = "Unable to process transaction. The account had Insufficient funds.";
            default:
                return x;
        }
    }
    private String payout(String respcode) 
    {
        String x = "System Error";
        switch (respcode)
        {
            case "0":
                return x = "Success";
            case "001":
                return x = "Cancelled";
            case "002":
                return x = "Already Claimed";
            case "003":
                return x = "Not Found";
            case "004":
                return x = "Blocked Transactions";
            case "005":
                return x = "Connection Error";
            default:
                return x;
        }
    }

    #endregion
}
