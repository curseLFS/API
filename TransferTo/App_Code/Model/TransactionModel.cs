using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for TransactionModel
/// </summary>
public class TransactionModel
{
    public BENEFICIARY beneficiary { get; set; }
    public DESTINATION destination { get; set; }
    public String purpose_of_remittance { get; set; }
    public SENDER sender { get; set; }
    public String transaction_code { get; set; }
}

public class BENEFICIARY 
{
    public String address { get; set; }
    public String bank_account_holder_name { get; set; }
    public String city { get; set; }
    public String code { get; set; }
    public String country_iso_code { get; set; }
    public String country_of_birth_iso_code { get; set; }
    public String date_of_birth { get; set; }
    public String email { get; set; }
    public String firstname { get; set; }
    public String gender { get; set; }
    public String id_country_iso_code { get; set; }
    public String id_delivery_date { get; set; }
    public String id_expiration_date { get; set; }
    public String id_number { get; set; }
    public String id_type { get; set; }
    public String lastname { get; set; }
    public String lastname2 { get; set; }
    public String middlename { get; set; }
    public String msisdn { get; set; }
    public String nationality_country_iso_code { get; set; }
    public String nativename { get; set; }
    public String occupation { get; set; }
    public String postal_code { get; set; }
    public String province_state { get; set; }

}

public class DESTINATION 
{
    public String amount { get; set; }
    public String currency { get; set; }
}

public class SENDER 
{
    public String address { get; set; }
    public String beneficiary_relationship { get; set; }
    public String city { get; set; }
    public String code { get; set; }
    public String country_iso_code { get; set; }
    public String country_of_birth_iso_code { get; set; }
    public String date_of_birth { get; set; }
    public String email { get; set; }
    public String firstname { get; set; }
    public String gender { get; set; }
    public String id_country_iso_code { get; set; }
    public String id_delivery_date { get; set; }
    public String id_expiration_date { get; set; }
    public String id_number { get; set; }
    public String id_type { get; set; }
    public String lastname { get; set; }
    public String Bertha { get; set; }
    public String lastname2 { get; set; }
    public String middlename { get; set; }
    public String msisdn { get; set; }
    public String nationality_country_iso_code { get; set; }
    public String nativename { get; set; }
    public String occupation { get; set; }
    public String postal_code { get; set; }
    public String province_state { get; set; }
    public String source_of_funds { get; set; }
}