$(document).ready(function () {
    // Delete Post
    $('#deletePostBtn').click(function (e) {
        e.preventDefault();
        const button = $(this);
        const id = button.data('id');
        const url = button.data('url');

        $.ajax({
            url: url,
            type: 'POST',
            data: { id },
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.success) {
                    window.location.href = response.redirect;
                }
            },
            error: function (xhr) {
                console.error("Lỗi AJAX:", xhr.responseText);
                alert("Đã xảy ra lỗi trong quá trình xử lý. Chi tiết: " + xhr.responseText);
            }
        });
    });

    // Pin Post
    $('#pinPostBtn').click(function (e) {
        e.preventDefault();
        const button = $(this);
        const id = button.data('id');
        const url = button.data('url');

        $.ajax({
            url: url,
            type: 'POST',
            data: { id },
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.success) {
                    button.text(response.isPinned ? "Bỏ ghim" : "Ghim");
                }
            },
            error: function (xhr) {
                console.error("Lỗi AJAX:", xhr.responseText);
                alert("Đã xảy ra lỗi trong quá trình xử lý. Chi tiết: " + xhr.responseText);
            }
        });
    });

    // Like Post
    $('#likePostBtn').click(function (e) {
        e.preventDefault();
        const button = $(this);
        const id = button.data('id');
        const url = button.data('url');

        $.ajax({
            url: url,
            type: 'POST',
            data: { id },
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.success) {
                    const icon = $(`#icon-like`);
                    const numLikesSpan = $(`#num-likes`);
                    if (icon.hasClass('far fa-heart')) {
                        icon.removeClass('far fa-heart').addClass('fas fa-heart');
                    } else {
                        icon.removeClass('fas fa-heart').addClass('far fa-heart');
                    }
                    numLikesSpan.text(response.numLikesFormatted);
                }
            },
            error: function (xhr) {
                console.error("Lỗi AJAX:", xhr.responseText);
                alert("Đã xảy ra lỗi trong quá trình xử lý. Chi tiết: " + xhr.responseText);
            }
        });
    });

    //Comment
    // Send Comment
    $('#commentForm').submit(function (e) {
        e.preventDefault();
        var content = $('#commentText').val();
        if (!content.trim()) {
            alert("Nội dung không được để trống!");
            return;
        }
        const form = $(this);
        const formData = form.serialize();
        const url = form.attr('action')

        $.ajax({
            url: url,
            type: 'POST',
            data: formData,
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                $('#commentsContainer').prepend(response);
                $('#commentText').val('');
            }
        });
    });

    // Reply Comment
    $(document).on('submit', '.replyForm', function (e) {
        e.preventDefault();
        const form = $(this);
        const url = form.attr('action')
        const content = form.find('.replyText').val();
        const parentId = form.data('parentid');
        const postId = form.data('postid')

        $.ajax({
            url: url,
            type: 'POST',
            data: { content: content, postId: postId, parentId: parentId },
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                form.after(response);
                form.hide();
                form.find('.replyText').val('');
            }
        });
    });

    // Toggle form reply
    $(document).on('click', '.replyBtn', function () {
        var button = $(this);
        var form = button.next('.replyForm');
        form.toggle();
    });

    // Load more comment
    var allComments = [];
    var commentsPerPage = 5;
    var currentIndex = 0;

    const url = $('#loadMoreCommentsBtn').data('url');
    const postId = $('#loadMoreCommentsBtn').data('id');

    $.ajax({
        url: url,
        type: 'GET',
        data: { postId: postId },
        success: function (response) {
            allComments = response;
            showNextComments();
        }
    });

    function renderComment(comment) {
        var repliesHtml = (comment.childComments || []).map(renderComment).join('');

        return`
                <div class="comment" data-id="${comment.id}" style="margin-left: ${comment.parentCommentId ? "40px" : "0px"};">
                    <strong>${comment.userName}</strong> - <span class="text-muted">${comment.dateCommented}</span>
                    <p>${comment.content}</p>
                    <button class="btn btn-link btn-sm replyBtn" data-id="${comment.Id}">Trả lời</button>
                    <form asp-action="ReplyComment" class="replyForm" style="display:none;"
                        data-parentid="${comment.id}" data-postid="${comment.postId}">
                        <textarea class="form-control replyText" rows="2" placeholder="Viết câu trả lời..."></textarea>
                        <button type="submit" class="btn btn-primary btn-sm mt-2">Gửi</button>
                    </form>
                    <div class="replies">${repliesHtml}</div>
                </div>
            `;
        };

    function showNextComments() {
        var commentsToShow = allComments.slice(currentIndex, currentIndex + commentsPerPage);

        commentsToShow.forEach(function (comment) {
            $('#commentsContainer').append(renderComment(comment));
        });

        currentIndex += commentsPerPage;

        if (currentIndex >= allComments.length) {
            $('#loadMoreCommentsBtn').hide();
        }
    }

    $('#loadMoreCommentsBtn').click(function () {
        showNextComments();
    });
});