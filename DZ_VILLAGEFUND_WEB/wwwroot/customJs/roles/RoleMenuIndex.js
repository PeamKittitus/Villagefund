$(function () {

    $("#Role").select2();
    $("#Role").change(function () {
        $("#JsonData").html('<img src="/img/loading.gif" width="200">');
        $.get("/Settings/GetRoleMenus", { "RoleId": $(this).val() }, function (JsonResult) {
            setTimeout(function () {

                // chaeck all
                $("#JsonData").html(JsonResult);
                $("#JsonData").on("change", "#all", function () {
                    $(".MenuItemId").prop('checked', $(this).prop("checked"));
                });

                // update data
                $("#JsonData #Submit").click( function () {
                    var RoleId = $("#RoleId").val();
                    var servicesCheckboxes = new Array();
                    $('input[name="MenuItemId"]:checked').each(function () {
                        servicesCheckboxes.push($(this).val());
                    });

                    $.post("/Settings/AddRoleMenu", { "MenuItemId": servicesCheckboxes, "RoleId": RoleId }, function (result) {
                        if (result.valid == true) {
                            toastr.success(result.message);
                            setTimeout(function () {
                                window.location.href = "/Settings/RoleMenuIndex";
                            }, 700);
                        } else {
                            toastr.error('Error!' + rs.message);
                            setTimeout(function () {
                                window.location.href = "";
                            }, 700);
                        }
                    })
                });


            }, 200);
        });
    });
    $("#Role").val($("#Role").val()).change();

});