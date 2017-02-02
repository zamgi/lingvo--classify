$(document).ready(function () {
    var MAX_INPUTTEXT_LENGTH  = 10000,
        LOCALSTORAGE_TEXT_KEY = 'classify-text',
        DEFAULT_TEXT          = 'Сегодня в App Store вышло обновленное приложение Яндекс.Перевода для iOS. Теперь в нем есть возможность полнотекстового перевода в офлайн-режиме. Машинный перевод прошел путь от мейнфреймов, занимавших целые комнаты и этажи, до мобильных устройств, помещающихся в карман. Сегодня полнотекстовый статистический машинный перевод, требовавший ранее огромных ресурсов, стал доступен любому пользователю мобильного устройства – даже без подключения к сети. Люди давно мечтают о «вавилонской рыбке» – универсальном компактном переводчике, который всегда можно взять с собой. И, кажется, мечта эта постепенно начинает сбываться. Мы решили, воспользовавшись подходящим случаем, подготовить небольшой экскурс в историю машинного перевода и рассказать о том, как развивалась эта интереснейшая область на стыке лингвистики, математики и информатики.';

    var textOnChange = function () {
        var _len = $("#text").val().length; 
        var len = _len.toString().replace(/\B(?=(\d{3})+(?!\d))/g, " ");
        var $textLength = $("#textLength");
        $textLength.html("длина текста: " + len + " символов");
        if (MAX_INPUTTEXT_LENGTH < _len) $textLength.addClass("max-inputtext-length");
        else                             $textLength.removeClass("max-inputtext-length");
    };
    var getText = function( $text ) {
        var text = trim_text( $text.val().toString() );
        if (is_text_empty(text)) {
            alert("Введите текст для обработки.");
            $text.focus();
            return (null);
        }
        
        if (text.length > MAX_INPUTTEXT_LENGTH) {
            if (!confirm('Превышен рекомендуемый лимит ' + MAX_INPUTTEXT_LENGTH + ' символов (на ' + (text.length - MAX_INPUTTEXT_LENGTH) + ' символов).\r\nТекст будет обрезан, продолжить?')) {
                return (null);
            }
            text = text.substr( 0, MAX_INPUTTEXT_LENGTH );
            $text.val( text );
            $text.change();
        }
        return (text);
    };

    $("#text").focus(textOnChange).change(textOnChange).keydown(textOnChange).keyup(textOnChange).select(textOnChange).focus();

    (function () {
        function isGooglebot() {
            return (navigator.userAgent.toLowerCase().indexOf('googlebot/') != -1);
        };
        if (isGooglebot())
            return;

        var text = localStorage.getItem(LOCALSTORAGE_TEXT_KEY);
        if (!text || !text.length) {
            text = DEFAULT_TEXT;
        }
        $('#text').text(text).focus();
    })();

    $('#mainPageContent').on('click', '#processButton', function () {
        if($(this).hasClass('disabled')) return (false);

        var text = getText( $("#text") );
        if (!text) return (false);

        processing_start();
        if (text != DEFAULT_TEXT) {
            localStorage.setItem(LOCALSTORAGE_TEXT_KEY, text);
        } else {
            localStorage.removeItem(LOCALSTORAGE_TEXT_KEY);
        }

        $.ajax({
            type: "POST",
            url:  "RESTProcessHandler.ashx",
            data: {
                text: text
            },
            success: function (responce) {
                if (responce.err) {
                    if (responce.err == "goto-on-captcha") {
                        window.location.href = "Captcha.aspx";
                    } else {
                        processing_end();
                        $('.result-info').addClass('error').text(responce.err);
                    }
                } else {
                    if (responce.classes && responce.classes.length != 0) {
                        $('.result-info').removeClass('error').text('');
                        text = '';
                        for (var i = 0, len = responce.classes.length; i < len; i++) {
                            var ci = responce.classes[i];
                            if (i == 0)
                                text += '<tr style="font-size: larger; font-weight: bold;"><td>' + ci.n + '</td><td>' + ci.p + '%</td></tr>';
                            else
                                text += '<tr><td>' + ci.n + '</td><td>' + ci.p + '%</td></tr>';
                        }
                        text += '<tr style="color: gray"><td>Другие</td><td>менее 10%</td></tr>';
                        $('#processResult tbody').html( text );
                        processing_end();
                        $('.result-info').hide();
                    } else {
                        processing_end();
                        $('.result-info').text('класс текста не определен');
                    }
                }
            },
            error: function () {
                processing_end();
                $('.result-info').text('ошибка сервера');
            }
        });
        
    });

    force_load_model();

    function processing_start(){
        $('#text').addClass('no-change').attr('readonly', 'readonly').attr('disabled', 'disabled');
        $('.result-info').show().removeClass('error').text('Идет обработка...');
        $('#processButton').addClass('disabled');
        $('#processResult tbody').empty();
    };
    function processing_end(){
        $('#text').removeClass('no-change').removeAttr('readonly').removeAttr('disabled');
        $('.result-info').removeClass('error').text('');
        $('#processButton').removeClass('disabled');
    };
    function trim_text(text) {
        return (text.replace(/(^\s+)|(\s+$)/g, ""));
    };
    function is_text_empty(text) {
        return (text.replace(/(^\s+)|(\s+$)/g, "") == "");
    };
    function force_load_model() {
        $.ajax({
            type: "POST",
            url: "RESTProcessHandler.ashx",
            data: { text: "_dummy_" }
        });
    };
});
