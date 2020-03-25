using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for complete
/// </summary>
[Serializable]
public class complete
{
    public String creation_date { get; set; }
    public String external_id { get; set; }
    public String id { get; set; }
    public String status { get; set; }
    public String status_class { get; set; }
    public String status_class_message { get; set; }
    public String status_message { get; set; }
    public branch branch { get; set; }
    public transaction transaction { get; set; }
}

public class transaction
{
    public sender sender { get; set; }
    public beneficiary beneficiary { get; set; }
    public destination destination { get; set; }
    public String purpose_of_remittance { get; set; }
    public String transaction_code { get; set; }
}

public class destination
{
    public int amount { get; set; }
    public String currency { get; set; }
}

public class sender
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

public class beneficiary
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