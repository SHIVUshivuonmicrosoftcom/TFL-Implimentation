function createCase(formContext) {
	try
	{
		//debugger;
		var parameters = {};
		parameters["title"] = formContext.getAttribute("sp_observationtitle").getValue();
		parameters["caseorigincode"] = 778390001;
		var lookupValue = new Array();
		lookupValue[0] = new Object();
		lookupValue[0].id = formContext.data.entity.getId();
		lookupValue[0].name = formContext.getAttribute("sp_observationtitle").getValue();
		lookupValue[0].entityType = "sp_observation";
		parameters["sp_observationid"] = lookupValue;
		var entityFormOptions = {};
		entityFormOptions["entityName"] = "incident";
		var record = {};
		record.statecode = 1; // State
		record.statuscode = 778390001; // Status
		Xrm.WebApi.updateRecord("sp_observation", formContext.data.entity.getId(), record).then(
			function success(result) {
			},
			function (error) {
			}
		);
		Xrm.Navigation.openForm(entityFormOptions, parameters).then(
			function (success) {

			},
			function (error) {
			});
		
	}

	catch (ex) {
	}
}
async function CreateCustomer(executionContext) {
	try {
		debugger;
		var formContext = executionContext.getFormContext();
		if (formContext.ui.getFormType() === 1) {
			if (formContext.getAttribute("sp_observationid").getValue() !== null) {
				var contactId = await getContactId(formContext, formContext.getAttribute("sp_observationid").getValue()[0].id.replace("{", "").replace("}", ""))

			}
		}


	}

	catch (ex) {
	}

}
async function getContactId(formContext, observationId) {
	try {
		var contactId = null;
		var firstName = null; var lastName = null;
		var emailAddress = null;
		var createdBy = null;
		var userfullname = null;
		debugger;
		var result = await Xrm.WebApi.retrieveRecord("sp_observation", observationId, "?$select=_createdby_value").then(
			function success(result) {
				// Columns
				var createdby = result["_createdby_value"]; // Lookup
				debugger;
				if (result["_createdby_value"] !== null) {
					createdBy = result["_createdby_value"];

				}

			},
			function (error) {
			}
		);

		if (createdBy !== null) {
			var userresult = await Xrm.WebApi.retrieveRecord("systemuser", createdBy, "?$select=_createdby_value,internalemailaddress,firstname,lastname,fullname").then(
				function success(result) {
					debugger;
					var internalemailaddress = result["internalemailaddress"]; // Text
					firstName = result["firstname"];
					lastName = result["lastname"];
					userfullname = result["fullname"];
					emailAddress = result["internalemailaddress"];
					if (result["internalemailaddress"] !== "") {
						emailAddress = result["internalemailaddress"];

					}
				},
				function (error) {
				}
			);
		}
		if (emailAddress !== null) {
			var contactres = await Xrm.WebApi.retrieveMultipleRecords("contact", "?$select=contactid,_createdby_value,emailaddress1,fullname&$filter=emailaddress1 eq '" + emailAddress + "'").then(
				function success(results) {
					debugger;
					for (var i = 0; i < results.entities.length; i++) {
						var result = results.entities[i];
						// Columns
						//return result["contactid"]; // Guid
						var lookupValue = new Array();
						lookupValue[0] = new Object();
						lookupValue[0].id = result["contactid"];
						lookupValue[0].name = result["fullname"];
						lookupValue[0].entityType = "contact";
						formContext.getAttribute("customerid").setValue(lookupValue);
					}
				},
				function (error) {
					console.log(error.message);
				}
			);

			var record = {};
			record.firstname = firstName; // Text
			record.lastname = lastName; // Text
			record.emailaddress1 = emailAddress; // Text

			var res = await Xrm.WebApi.createRecord("contact", record).then(
				function success(result) {
					contactId = result.id;
					var lookupValue = new Array();
					lookupValue[0] = new Object();
					lookupValue[0].id = result.id;;
					lookupValue[0].name = userfullname;
					lookupValue[0].entityType = "contact";
					formContext.getAttribute("customerid").setValue(lookupValue);
				},
				function (error) {
				}
			);

		}

	}

	catch (ex) {
	}

}