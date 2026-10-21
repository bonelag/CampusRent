import { Component } from 'react';
export default class ErrorBoundary extends Component {
  state = { failed: false };
  static getDerivedStateFromError() {
    return { failed: true };
  }
  componentDidCatch(error) {
    console.error('CampusRent rendering error:', error);
  }
  render() {
    return this.state.failed ? (
      <main className="container page-shell">
        <section className="error-state" role="alert">
          <h1>Trang đang gặp một chút trục trặc</h1>
          <p>Vui lòng tải lại trang để tiếp tục tìm phòng.</p>
          <button className="button primary" onClick={() => window.location.reload()}>
            Tải lại trang
          </button>
        </section>
      </main>
    ) : (
      this.props.children
    );
  }
}
