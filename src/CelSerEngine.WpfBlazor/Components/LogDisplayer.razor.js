const logMessagesEl = document.getElementById('logMessages');
document.querySelector("[data-bs-target='#console-tab-pane']").addEventListener('click', function () {
    scrollToBottom();
});


export function scrollToBottom() {
    setTimeout(() => {
        logMessagesEl.scrollTop = logMessagesEl.scrollHeight;
    }, 100);
}