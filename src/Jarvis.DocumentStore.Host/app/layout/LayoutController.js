(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.layout').controller('Layout', Layout);

    // Lyaout.$inject = ['']

    function Layout() {
        var vm = this;

        vm.components = {
            header: '/layout/header.html'
        };
    }
})(window, window.angular);
