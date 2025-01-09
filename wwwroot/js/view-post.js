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
        const isAuthenticated = button.data('is-authenticated');
        
        if (!isAuthenticated) {
            alert("Bạn cần đăng nhập để like bài viết");
            return;
        }

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

    //Bookmark Post
    $('#bookmarkPostBtn').click(function (e) {
        e.preventDefault();
        const button = $(this);
        const id = button.data('id');
        const url = button.data('url');
        const isAuthenticated = button.data('is-authenticated');

        if (!isAuthenticated) {
            alert("Bạn cần đăng nhập để sử dụng tính năng thêm bookmark.");
            return;
        }

        $.ajax({
            url: url,
            type: 'POST',
            data: { id },
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.success) {
                    const icon = $(`#icon-bookmark`);
                    if (icon.hasClass('far fa-bookmark')) {
                        icon.removeClass('far fa-bookmark').addClass('fas fa-bookmark');
                    } else {
                        icon.removeClass('fas fa-bookmark').addClass('far fa-bookmark');
                    }
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
        if (!isUserLoggedIn) {
            alert("Bạn cần đăng nhập để thực hiện bình luận!");
            return;
        }

        var content = $('#commentText').val();
        const numCommentsSpan = $(`.num-comments`);
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
                if (response.success) {
                    $('#comments-container').prepend(response.html);
                    $('#commentText').val('');
                    numCommentsSpan.text(response.numCommentsFormatted);
                }
            }
        });
    });

    // Reply Comment
    $(document).on('submit', '.replyForm', function (e) {
        e.preventDefault();
        if (!isUserLoggedIn) {
            alert("Bạn cần đăng nhập để thực hiện phản hồi!");
            return;
        }

        const form = $(this);
        const url = '/ReplyComment'
        const content = form.find('.replyText').val();
        const parentId = form.data('parentid');
        const postId = form.data('postid')
        const numCommentsSpan = $(`.num-comments`);

        $.ajax({
            url: url,
            type: 'POST',
            data: { content: content, postId: postId, parentId: parentId },
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.success) {
                    form.after(response.html);
                    form.hide();
                    form.find('.replyText').val('');
                    numCommentsSpan.text(response.numCommentsFormatted);
                }
            }
        });
    });

    // Toggle form reply
    $(document).on('click', '.replyBtn', function () {
        if (!isUserLoggedIn) {
            alert("Bạn cần đăng nhập để thực hiện phản hồi!");
            return;
        }
        var button = $(this);
        var form = button.next('.replyForm');
        form.toggle();
    });

    // Load more comment
    let allComments = [];
    let visibleCount = 5;

    const url = $('#loadMoreBtn').data('url');
    const postId = $('#loadMoreBtn').data('id');

    function renderComment(comment) {
        let childCommentsHtml = "";
        if (comment.childComments.length > 0) {
            childCommentsHtml = `
                <div class="child-comments" id="child-comments-${comment.id}" style="display:none;">
                    ${comment.childComments.map(renderComment).join('')}
                </div>
                <button class="toggle-child-btn" data-id="${comment.id}"><i class="far fa-eye"></i> Xem phản hồi</button>
            `;
        }

        return `
            <div class="comment" data-id="${comment.id}" style="margin-left: ${comment.parentCommentId ? "40px" : "0px"};">
                <div class="d-flex">
                    <img src="${comment.avatarPath}" alt="Avatar tác giả" class="author-avatar me-3">
                    <a href="/profile/${comment.userId}" class="username">${comment.userName}</a>
                    <span class="text-muted date-comment">&nbsp;&nbsp;&bull;&nbsp;&nbsp;${comment.dateCommented}</span>
                </div>
                <p class="comment-content">${comment.content}</p>
                <button class="replyBtn" data-id="${comment.Id}"><i class="far fa-comment"></i> Phản hồi</button>
                <form asp-action="ReplyComment" class="replyForm" style="display:none;"
                    data-parentid="${comment.id}" data-postid="${comment.postId}">
                    <textarea class="form-control replyText" rows="2" placeholder="Viết phản hồi..."></textarea>
                    <button type="submit" class="btn-send-reply"><p>Gửi</p></button>
                </form>
                ${childCommentsHtml}
            </div>
        `;
    }

    function loadMoreComments() {
        const container = $('#comments-container');
        const commentsToShow = allComments.slice(0, visibleCount).map(renderComment).join('');
        container.html(commentsToShow);

        if (visibleCount >= allComments.length) {
            $('#loadMoreBtn').hide();
        } else {
            $('#loadMoreBtn').show();
        }
    }

    $.ajax({
        url: url,
        type: 'GET',
        data: { postId: postId },
        success: function (response) {
            allComments = response;
            loadMoreComments();
        }
    });

    $('#loadMoreBtn').click(function () {
        visibleCount += 5;
        loadMoreComments();
    });

    $(document).on('click', '.toggle-child-btn', function () {
        const commentId = $(this).data('id');
        const childContainer = $(`#child-comments-${commentId}`);
        childContainer.toggle();
        if (childContainer.is(':visible')) {
            $(this).html(`<i class="far fa-eye-slash"></i> Ẩn phản hồi`);
        } else {
            $(this).html(`<i class="far fa-eye"></i> Xem phản hồi`);
        }
    });

    //Share post
    function copyToClipboard() {
        const currentUrl = window.location.href;
        navigator.clipboard.writeText(currentUrl).then(() => {
            alert('Link bài viết đã được sao chép vào bộ nhớ tạm!');
        }).catch(err => {
            console.error('Lỗi khi sao chép:', err);
        });
    }
    
    window.copyToClipboard = copyToClipboard;
});