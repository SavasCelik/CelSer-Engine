let modalApi;

export function initModal(modalId) {
    const modalEl = document.getElementById(modalId);
    modalApi = new bootstrap.Modal(modalEl);

    modalEl.addEventListener('show.bs.modal', event => {
        setTimeout(() => modalEl.querySelector("input")?.focus(), 500);
    });
}

export function showModal() {
    modalApi.show();
}