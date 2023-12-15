
/*
 *  get data 
 */

$(function () {
    $("#JsonData").html('<img src="/img/loading.gif" width="200">');
    $.get("/EOffice/GetOrganizations", function (JsonResult) {
        setTimeout(function () {
            $("#JsonData").html(JsonResult);

            // edit
            $("#JsonData").on("click", ".edit", function () {
                $.get("/EOffice/FormEditOrg", { "OrgId": $(this).val() }, function (JsonForm) {
                    $("#JsonEditData").html(JsonForm);
                    $("#FormEditOrgModal").modal('show');

                    $('#JsonForm').bootstrapValidator();
                    $("#Submit").click(function () {
                        $('#JsonForm').data("bootstrapValidator").validate();
                        if ($('#JsonForm').data("bootstrapValidator").isValid() == true) {
                            var Data = new FormData($("#JsonForm")[0]); JsonForm
                            $.ajax(
                                {
                                    type: "POST",
                                    url: "/EOffice/FormEditOrg",
                                    contentType: false,
                                    processData: false,
                                    data: Data,
                                    success: function (result) {
                                        if (result.valid == true) {
                                            toastr.success(result.message);
                                            setTimeout(function () {
                                                window.location.href = "";
                                            }, 200);
                                        }
                                        else {
                                            toastr.error('Error!' + result.message);
                                            setTimeout(function () {
                                                window.location.href = "";
                                            }, 700);
                                        }
                                    }
                                });
                        }
                    });
                });
            });

            // delete
            $("#JsonData").on("click", ".delete", function () {
                const Id = $(this).val();
                Swal.fire(
                    {
                        title: "ยืนยันการทำรายการ",
                        text: "คุณต้องการลบรายการนี้ หรือไม่?",
                        type: "warning",
                        showCancelButton: true,
                        confirmButtonText: "ยืนยัน",
                        cancelButtonText: "ยกเลิก",
                        closeOnConfirm: false
                    }).then(function (isConfirm) {
                        if (isConfirm.value == true) {
                            $.get("/EOffice/DeleteOrg", { "OrgId": Id }, function (rs) {
                                if (rs.valid == true) {
                                    toastr.success(rs.message);
                                    setTimeout(function () {
                                        window.location.href = "";
                                    }, 200);
                                }
                                else {
                                    toastr.error('Error!' + rs.message);
                                }
                            });
                        }
                    });
            });

            // add user to org
            $("#JsonData").on("click", ".addUser", function () {
                $.get("/EOffice/FromAddUserOrg", { "OrgId": $(this).val() }, function (JsonForm) {
                    $("#JsonAddUserData").html(JsonForm);
                    $("#FormAddUserOrgModal").modal('show');

                    $("#FormAddUserOrgModal #UserId").select2({ dropdownParent: $("#FormAddUserOrgModal") });

                    $('#JsonForm').bootstrapValidator();
                    $("#Submit").click(function () {
                        $('#JsonForm').data("bootstrapValidator").validate();
                        if ($('#JsonForm').data("bootstrapValidator").isValid() == true) {
                            var Data = new FormData($("#JsonForm")[0]); JsonForm
                            $.ajax(
                                {
                                    type: "POST",
                                    url: "/EOffice/FromAddUserOrg",
                                    contentType: false,
                                    processData: false,
                                    data: Data,
                                    success: function (result) {
                                        if (result.valid == true) {
                                            toastr.success(result.message);
                                            setTimeout(function () {
                                                window.location.href = "";
                                            }, 200);
                                        }
                                        else {
                                            toastr.error('Error!' + result.message);
                                            setTimeout(function () {
                                                window.location.href = "";
                                            }, 700);
                                        }
                                    }
                                });
                        }
                    });
                });
            });

            // view user
            $("#JsonData").on("click", ".viewUser", function () {
                $.get("/EOffice/ViewUsers", { "OrgId": $(this).val() }, function (JsonForm) {
                    $("#JsonViewUserData").html(JsonForm);
                    $("#FormViewUserOrgModal").modal('show');

                    $("#FormViewUserOrgModal").on("click", ".deleteUser", function () {
                        const OrgUserId = $(this).attr("data-id");
                        $.get("/EOffice/DeleteUserOrg", { "OrgUserId": OrgUserId }, function (rs) {
                            if (rs.valid == true) {
                                toastr.success(rs.message);
                                setTimeout(function () {
                                    window.location.href = "";
                                }, 200);
                            }
                            else {
                                toastr.error('Error!' + rs.message);
                            }
                        });
                    });
                });
            });

            // update position
            var updateOutput = function (e) {
                var list = e.length ? e : $(e.target), output = list.data('output');
                if (window.JSON) {
                    $(".dd").change(function () {
                        $.ajax({
                            dataType: 'JSON',
                            type: 'POST',
                            url: "/EOffice/UpdatePosition",
                            data: "Position=" + JSON.stringify(list.nestable('serialize')),
                            success: function (result) {
                                if (result.valid == true) {
                                    toastr.success(result.message);
                                    setTimeout(function () {
                                        window.location.href = "";
                                    }, 200);
                                }
                                else {
                                    toastr.error('Error!' + result.message);
                                }
                            }
                        });
                    });

                } else {
                    output.val('JSON browser support required for this demo.');
                }
            };

            $('#nestable3').nestable({
                group: 1
            }).on('change', updateOutput);

            updateOutput($('#nestable3').data('output', $('#nestable3-output')));

            $('#nestable3').nestable();

        }, 200);
    });

    // add 
    $("#add").click(function () {
        $.get("/EOffice/FormAddOrg", function (JsonForm) {
            $("#JsonAddData").html(JsonForm);
            $("#FormAddOrgModal").modal('show');

            $('#JsonForm').bootstrapValidator();
            $("#Submit").click(function () {
                $('#JsonForm').data("bootstrapValidator").validate();
                if ($('#JsonForm').data("bootstrapValidator").isValid() == true) {
                    var Data = new FormData($("#JsonForm")[0]); JsonForm
                    $.ajax(
                        {
                            type: "POST",
                            url: "/EOffice/FormAddOrg",
                            contentType: false,
                            processData: false,
                            data: Data,
                            success: function (result) {
                                if (result.valid == true) {
                                    toastr.success(result.message);
                                    setTimeout(function () {
                                        window.location.href = "";
                                    }, 200);
                                }
                                else {
                                    toastr.error('Error!' + result.message);
                                    setTimeout(function () {
                                        window.location.href = "";
                                    }, 700);
                                }
                            }
                        });
                }
            });
        });
    })
});