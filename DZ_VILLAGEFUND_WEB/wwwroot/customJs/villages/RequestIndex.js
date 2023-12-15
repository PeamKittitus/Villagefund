$(function () {

    $("#TransactionType").select2();
    $("#TransactionType").change(function () {
        $("#JsonData").html('<img  src="/img/loading.gif" width="200">');
        $.get("/Villages/GetRequest", { "TransactionType": $("#TransactionType").val() }, function (JsonResult) {
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

            // edit
            $('#JsonTable .edit').unbind().click(function () {
                window.location.href = "/Villages/FormAddVillage";
            });
        });
    });
    $("#TransactionType").val($("#TransactionType").val()).change();
});