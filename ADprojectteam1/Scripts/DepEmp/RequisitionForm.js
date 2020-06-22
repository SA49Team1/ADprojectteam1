var RequisitionFormFW = {
    Parameter1: null,
    Parameter2: null,
    Initialize: function () {


        window.onload = function () { RequisitionFormFW.searchEnter() };
    },
    searchEnter: function () {
        var searchBox = document.getElementById("searchBox");
        searchBox.addEventListener("keypress", function (e) {
            var key = e.which || e.keyCode;
            if (key === 13) {
                var data = { searchStr: searchBox.value };  /*Here we pass the Viariable name "searchStr" and its value to dictionary 'data'*/
                RequisitionFormFW.doPOST(data);           /*Here we post 'data' to method, if the method has a parameter with same name of "searchStr", it passes to controller.*/
            }
        });
    },
    searchClick: function () {
        var searchBox = document.getElementById("searchBox");
        var data = { searchStr: searchBox.value };  /*Here we pass the Viariable name "searchStr" and its value to dictionary 'data'*/

        RequisitionFormFW.doPOST(data);
    },

    doPOST: function (data) {
        var ajax = new XMLHttpRequest();

        ajax.onreadystatechange = function () {
            if (ajax.readyState == 4) {
                document.open();
                document.write(ajax.response);
                document.close();
            }
        }
        ajax.open("POST", "/DepEmp/RequisitionForm", true);
        ajax.setRequestHeader("Content-type", "application/json;charset=UTF-8");

        ajax.send(JSON.stringify(data));
    },
    addReqItem: function (item_id) {
        var q = $('#quantityBox'+item_id).val();
        
        var pdata = { Id: item_id, quant: parseInt(q) };

        $.ajax({
            type: "POST",
            url: "/DepEmp/AddReqItem",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(pdata),
            dataType: "json",

            // Response Success
            success: function (response) {

                alert("Item added to Requisition form successfully");
                window.location.href = "./RequisitionForm";

            },

            // Response Fail
            failure: function (response) {
                alert("Fail Case");
            },

            // Response Error
            error: function (response) {
                alert("ERROR case");
            }


        });
    },
};



    $(document).ready(function () {
        RequisitionFormFW.Initialize();
    });


