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
        function isGooglebot() { return (navigator.userAgent.toLowerCase().indexOf('googlebot/') !== -1); };
        if (isGooglebot()) return;

        var text = localStorage.getItem(LOCALSTORAGE_TEXT_KEY);
        if (!text || !text.length) text = DEFAULT_TEXT;
        $('#text').val(text).focus();
    })();
    $('#resetText2Default').click(function () {
        $("#text").val('');
        setTimeout(function () { $("#text").val(DEFAULT_TEXT).focus(); }, 100);
    });

    $('#processButton').click(function () {
        if($(this).hasClass('disabled')) return (false);

        var text = getText( $("#text") );
        if (!text) return (false);

        processing_start();
        if (text !== DEFAULT_TEXT) {
            localStorage.setItem(LOCALSTORAGE_TEXT_KEY, text);
        } else {
            localStorage.removeItem(LOCALSTORAGE_TEXT_KEY);
        }

        var model = {
            text: text
        };
        $.ajax({
            type       : "POST",
            contentType: "application/json",
            dataType   : "json",
            url        : "/Process/Run",
            data       : JSON.stringify( model ),
            success: function (responce) {
                if (responce.err) {
                    if (responce.err === "goto-on-captcha") {
                        window.location.href = "/Captcha/GetNew";
                    } else {
                        processing_end();
                        $('.result-info').addClass('error').text(responce.err);
                    }
                } else {
                    if (responce.classes && responce.classes.length) {
                        $('.result-info').removeClass('error').text('');
                        var texts = [];
                        for (var i = 0, len = responce.classes.length; i < len; i++) {
                            var ci = responce.classes[i];
                            if (i === 0)
                                texts.push( '<tr style="font-size: larger; font-weight: bold;"><td>' + ci.n + '</td><td>' + ci.p + '%</td></tr>' );
                            else
                                texts.push( '<tr><td>' + ci.n + '</td><td>' + ci.p + '%</td></tr>' );
                        }
                        texts.push('<tr style="color: gray"><td>Другие</td><td>менее 10%</td></tr>');
                        setTimeout( () => $('#processResult tbody').html( texts.join('') ), 100 );
                        //$('#processResult tbody').html( texts.join('') );
                        processing_end();
                        $('.result-info').hide();
                    } else {
                        processing_end();
                        $('.result-info').text('Класс текста не определен');
                    }
                }
            },
            error: function () {
                processing_end();
                $('.result-info').text('ошибка сервера');
            }
        });
    });

    function processing_start(){
        $('#text').addClass('no-change').attr('readonly', 'readonly').attr('disabled', 'disabled');
        $('.result-info').show().removeClass('error').html('Идет обработка... <label id="processingTickLabel"></label>');
        $('#processButton').addClass('disabled');
        $('#processResult tbody').empty();
        setTimeout(processing_tick, 1000);
    };
    function processing_end(){
        $('#text').removeClass('no-change').removeAttr('readonly').removeAttr('disabled');
        $('.result-info').removeClass('error').text('');
        $('#processButton').removeClass('disabled');
    };
    function trim_text(text) { return (text.replace(/(^\s+)|(\s+$)/g, "")); };
    function is_text_empty(text) { return (!trim_text(text)); };

    var processingTickCount = 1;
    function processing_tick() {
        var n2 = function (n) {
            n = n.toString();
            return ((n.length === 1) ? ('0' + n) : n);
        }
        var d = new Date(new Date(new Date(new Date().setHours(0)).setMinutes(0)).setSeconds(processingTickCount));
        var t = n2(d.getHours()) + ':' + n2(d.getMinutes()) + ':' + n2(d.getSeconds()); //d.toLocaleTimeString();
        var $s = $('#processingTickLabel');
        if ($s.length) {
            $s.text(t);
            processingTickCount++;
            setTimeout(processing_tick, 1000);
        } else {
            processingTickCount = 1;
        }
    };
});
