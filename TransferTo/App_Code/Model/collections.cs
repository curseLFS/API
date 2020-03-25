using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for CreateTransactionModel
/// </summary>

[Serializable]
public class collections
{		
	public String external_id    { get; set; }	
	public branch branch     { get; set; }
    public String transaction_code { get; set; }
}
public class branch
{
    public String code  { get; set; }
	public String name  { get; set; }
	public String address  { get; set; }
	public String postal_code  { get; set; }
	public String city  { get; set; }
	public String country_iso_code  { get; set; }
}
