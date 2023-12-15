

/*
 * create by bunchuai
 * date 19-01-2023
 */


$(function () {

    $("#Month").select2();
    $("#Year").select2();

    var Select = $("#Month, #Year");
    Select.change(function () {
        $("#JsonData").html('<img  src="/img/loading.gif" width="200">');
        $.get("/Settings/GetLogs", { "Month": $("#Month").val(), "Year": $("#Year").val() }, function (JsonResult) {
            setTimeout(function () {
                $("#JsonData").html(JsonResult);
                $('#JsonTable').dataTable(
                    {
                        responsive: true,
                        lengthChange: false,
                        dom: "<'row mb-3'<'col-sm-12 col-md-6 d-flex align-items-center justify-content-start'f><'col-sm-12 col-md-6 d-flex align-items-center justify-content-end'lB>>" +
                            "<'row'<'col-sm-12'tr>>" +
                            "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",
                        buttons: [
                            {
                                extend: 'print',
                                text: 'สั่งพิมพ์',
                                titleAttr: 'Print Table',
                                className: 'btn-outline-primary btn-sm'
                            }
                        ]
                    });
            }, 200);
        });
    });
    $("#Month").val($("#Month").val()).change();
});