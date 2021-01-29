$(function () {

    $('#emojis-container').on('click', 'a', function () {
        var value = $("input", $(this)).val();
        var input = $('#chat-message');
        input.val(input.val() + value);
        input.focus();
        input.change();
    });

    // Show/Hide Emoji Window
    $("#emojibtn").click(function () {
        var x = $("#emojis-container");
        if (x.hasClass("hidden")) {
            x.removeClass("hidden");
        }
        else {
            x.addClass("hidden");
        }
    });

    $("#chat-message, #btn-send-message").click(function () {
        $("#emojis-container").addClass("hidden")
    });

    $('.modal').on('hidden.bs.modal', function () {
        $("input").val("");
    });
});