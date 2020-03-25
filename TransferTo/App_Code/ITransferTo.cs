using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

// NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService" in both code and config file together.
[ServiceContract]
public interface ITransferTo
{

    [OperationContract]
    [WebInvoke(Method = "GET",
        ResponseFormat = WebMessageFormat.Json,
        BodyStyle = WebMessageBodyStyle.WrappedRequest,
        UriTemplate = "searchTransaction/?refno={refno}")]
    TransResponse searchTransaction(String refno);

    [OperationContract]
    [WebInvoke(Method = "GET",
        ResponseFormat = WebMessageFormat.Json,
        BodyStyle = WebMessageBodyStyle.WrappedRequest,
        UriTemplate = "InquireTransaction/?refno={refno}")]
    String InquireTransaction(String refno);

    [OperationContract]
    [WebInvoke(Method = "GET",
        ResponseFormat = WebMessageFormat.Json,
        BodyStyle = WebMessageBodyStyle.WrappedRequest,
        UriTemplate = "retrieveCollection/?id={id}")]
    TransResponse retrieveCollection(String id);
   
    [OperationContract]
    [WebInvoke(Method = "POST",
        ResponseFormat = WebMessageFormat.Json,
        RequestFormat = WebMessageFormat.Json,
        BodyStyle = WebMessageBodyStyle.WrappedRequest,
        UriTemplate = "saveTransaction")]
    TransResponse saveTransaction(String refno, String sendername, String receivername, String receiveraddress, String receivercontact, String currency, String FXAmount, String transpin, String sec_Token, String series, Boolean isremote, String remoteOperator, String remotebcode, Int16 remotezcode, String remotereason, String bcode, Int16 zcode, String operatorID, String stationno, String ben_IDType, String ben_IDNo, String expirydate, String transtype, String POControl);

    [OperationContract]
    [WebInvoke(Method = "POST",
        ResponseFormat = WebMessageFormat.Json,
        BodyStyle = WebMessageBodyStyle.WrappedRequest,
        UriTemplate = "/commitToPartners")]
    String commitToPartners(String refno, String kptn, String bcode);

//    [OperationContract]
//    [WebInvoke(Method = "GET",
//        ResponseFormat = WebMessageFormat.Json,
//        BodyStyle = WebMessageBodyStyle.WrappedRequest,
//        UriTemplate = "/generateKPTNpartners/?branchcode={branchcode}&zonecode={zonecode}")]
//    String generateKPTNpartners(string branchcode, Int16 zonecode);
}


[DataContract]
public class TransResponse
{
    [DataMember]
    public String respcode { get; set; }
    [DataMember]
    public String respmsg { get; set; }

    [DataMember]
    public String transdate { get; set; }
    [DataMember]
    public String partnersdata { get; set; }
    [DataMember]
    public Details searchdata { get; set; }
}

[DataContract]
public class PartnersData 
{
    public String refno { get; set; }
    public String SenderFullName { get; set; }
    public String ReceiverFullName { get; set; }
    public String ReceiverContactNo { get; set; }
    public String ReceiverAddress { get; set; }
    public String Currency { get; set; }
    public String Amount { get; set; }
}

[DataContract]
public class Details
{
    [DataMember]
    public String status { get; set; }
    [DataMember]
    public String AccntCode { get; set; }
    [DataMember]
    public String RefNo { get; set; }   
    [DataMember]
    public String SenderFullName { get; set; }
    [DataMember]
    public String SenderAddress { get; set; }
    [DataMember]
    public String SenderContactNo { get; set; }
    [DataMember]
    public String SenderBirthDate { get; set; }
    [DataMember]
    public String SenderCity { get; set; }
    [DataMember]
    public String SenderCountry { get; set; }
    [DataMember]
    public String ReceiverFirstName { get; set; }
    [DataMember]
    public String ReceiverLastName { get; set; }
    [DataMember]
    public String ReceiverMiddleName { get; set; }
    [DataMember]
    public String ReceiverFullName { get; set; }
    [DataMember]
    public String ReceiverAddress { get; set; }
    [DataMember]
    public String ReceiverContactNo { get; set; }
    [DataMember]
    public String ReceiverBirthDate { get; set; }
    [DataMember]
    public String ReceiverCity { get; set; }
    [DataMember]
    public String ReceiverCountry { get; set; }
    [DataMember]
    public String Amount { get; set; }
    [DataMember]
    public String Currency { get; set; }
}

public class generateControlResponsePO
{

    public Int32 respcode { get; set; }
    public String message { get; set; }
    public String controlno { get; set; }
    public String nseries { get; set; }
    public String ErrorDetail { get; set; }
}


